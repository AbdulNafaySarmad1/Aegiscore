using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using AegisCore.Application.Finance;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed record VoucherImportResult(int Imported, IReadOnlyList<string> Errors, IReadOnlyList<Guid> VoucherIds);

public sealed class VoucherImportService(AegisDbContext db, IFinanceModuleService finance)
{
    public async Task<VoucherImportResult> ImportAsync(Guid tenantId, Guid actorId, BulkVoucherImportCommand command, CancellationToken ct)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(command.Base64Content); }
        catch (FormatException) { throw new ArgumentException("Voucher import content must be base64 encoded."); }
        if (bytes.Length is 0 or > 10_000_000) throw new ArgumentException("Voucher import must be between 1 byte and 10 MB.");
        var rows = command.Format.Trim().ToLowerInvariant() switch
        {
            "csv" => ParseCsv(System.Text.Encoding.UTF8.GetString(bytes)),
            "xlsx" => ParseXlsx(bytes),
            _ => throw new ArgumentException("Voucher import format must be csv or xlsx.")
        };
        if (rows.Count is 0 or > 20_000) throw new ArgumentException("Voucher import must contain between 1 and 20,000 lines.");
        var codes = rows.Select(x => Required(x, "account_code")).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var accounts = await db.ChartOfAccounts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && codes.Contains(x.AccountCode)).ToDictionaryAsync(x => x.AccountCode, StringComparer.OrdinalIgnoreCase, ct);
        var errors = new List<string>();
        var ids = new List<Guid>();
        foreach (var group in rows.GroupBy(x => Required(x, "voucher_ref"), StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var first = group.First();
                var lines = group.Select((row, index) =>
                {
                    var code = Required(row, "account_code");
                    if (!accounts.TryGetValue(code, out var account)) throw new ArgumentException($"Unknown account code {code} at voucher {group.Key} line {index + 1}.");
                    return new VoucherLineCommand(account.Id, OptionalGuid(row, "cost_center_id"), Money(row, "debit"), Money(row, "credit"), Optional(row, "line_description"));
                }).ToList();
                var voucher = await finance.CreateVoucherAsync(tenantId, actorId, new CreateVoucherCommand(Required(first, "voucher_type"), Required(first, "currency"), Optional(first, "description"), lines), ct);
                ids.Add(voucher.Id);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { errors.Add($"{group.Key}: {ex.Message}"); }
        }
        return new(ids.Count, errors, ids);
    }

    private static List<Dictionary<string, string>> ParseCsv(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return [];
        var headers = CsvLine(lines[0]).Select(x => x.Trim().ToLowerInvariant()).ToArray();
        return lines.Skip(1).Select(line => headers.Zip(CsvLine(line), (key, value) => (key, value)).ToDictionary(x => x.key, x => x.value, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static List<string> CsvLine(string line)
    {
        var values = new List<string>();
        var value = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"' && quoted && index + 1 < line.Length && line[index + 1] == '"') { value.Append('"'); index++; }
            else if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(character);
        }
        if (quoted) throw new ArgumentException("CSV contains an unterminated quoted field.");
        values.Add(value.ToString());
        return values;
    }

    private static List<Dictionary<string, string>> ParseXlsx(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var shared = ReadXml(archive, "xl/sharedStrings.xml", optional: true)?.Descendants().Where(x => x.Name.LocalName == "si").Select(x => string.Concat(x.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value))).ToArray() ?? [];
        var sheet = ReadXml(archive, "xl/worksheets/sheet1.xml")!;
        var matrix = sheet.Descendants().Where(x => x.Name.LocalName == "row").Take(20_001).Select(row => row.Elements().Where(x => x.Name.LocalName == "c").ToDictionary(
            cell => Column(cell.Attribute("r")?.Value ?? "A1"),
            cell => CellValue(cell, shared))).ToList();
        if (matrix.Count < 2) return [];
        var headers = matrix[0].ToDictionary(x => x.Key, x => x.Value.Trim().ToLowerInvariant());
        return matrix.Skip(1).Select(row => row.Where(x => headers.ContainsKey(x.Key)).ToDictionary(x => headers[x.Key], x => x.Value, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static XDocument? ReadXml(ZipArchive archive, string path, bool optional = false)
    {
        var entry = archive.GetEntry(path);
        if (entry is null) { if (optional) return null; throw new ArgumentException("XLSX worksheet is missing."); }
        if (entry.Length > 20_000_000) throw new ArgumentException("XLSX worksheet is too large.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20_000_000 });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static string CellValue(XElement cell, IReadOnlyList<string> shared)
    {
        var raw = cell.Descendants().FirstOrDefault(x => x.Name.LocalName == "v")?.Value ?? string.Concat(cell.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value));
        if (cell.Attribute("t")?.Value == "s" && int.TryParse(raw, out var index) && index >= 0 && index < shared.Count) return shared[index];
        return raw;
    }

    private static string Column(string reference) => new(reference.TakeWhile(char.IsLetter).ToArray());
    private static string Required(IReadOnlyDictionary<string, string> row, string name) => row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new ArgumentException($"{name} is required.");
    private static string? Optional(IReadOnlyDictionary<string, string> row, string name) => row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    private static Guid? OptionalGuid(IReadOnlyDictionary<string, string> row, string name) => Optional(row, name) is { } value ? Guid.Parse(value) : null;
    private static decimal Money(IReadOnlyDictionary<string, string> row, string name) => decimal.TryParse(Optional(row, name) ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : throw new ArgumentException($"{name} is invalid.");
}
