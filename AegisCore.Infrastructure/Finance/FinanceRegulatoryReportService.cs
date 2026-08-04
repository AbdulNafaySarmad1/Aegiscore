using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceRegulatoryReportService(AegisDbContext db, IFinanceModuleService finance, IPqcSignatureService pqc, IR2StorageService storage)
{
    private static readonly HashSet<string> Formats = ["JSON", "CSV", "XML", "XBRL", "IXBRL"];

    public async Task<IReadOnlyList<FinanceRegulatorySchema>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        await SeedAsync(tenantId, ct);
        return await db.FinanceRegulatorySchemas.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive).OrderBy(x => x.Code).ToListAsync(ct);
    }

    public async Task<FinanceRegulatorySchema> ConfigureAsync(Guid tenantId, ConfigureRegulatorySchemaCommand command, CancellationToken ct)
    {
        var code = Required(command.Code, 30, "Schema code").ToUpperInvariant();
        var format = Required(command.Format, 10, "Format").ToUpperInvariant();
        if (!Formats.Contains(format)) throw new ArgumentException("Regulatory schema format is unsupported.");
        XmlConvert.VerifyName(Required(command.RootElement, 100, "Root element"));
        ValidateDefinition(command.DefinitionJson);
        var active = await db.FinanceRegulatorySchemas.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.Code == code && x.IsActive).ToListAsync(ct);
        active.ForEach(x => x.IsActive = false);
        var schema = new FinanceRegulatorySchema { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Jurisdiction = Required(command.Jurisdiction, 30, "Jurisdiction").ToUpperInvariant(), Version = Required(command.Version, 30, "Version"), Format = format, RootElement = command.RootElement.Trim(), NamespaceUri = Optional(command.NamespaceUri, 500), DefinitionJson = CanonicalJson(command.DefinitionJson) };
        db.FinanceRegulatorySchemas.Add(schema);
        await db.SaveChangesAsync(ct);
        return schema;
    }

    public async Task<FinanceRegulatorySubmission> GenerateAsync(Guid tenantId, Guid actorId, string code, GenerateRegulatorySubmissionCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || command.From == default || command.To <= command.From) throw new ArgumentException("Report period and actor are required.");
        await SeedAsync(tenantId, ct);
        var normalized = Required(code, 30, "Schema code").ToUpperInvariant();
        var schema = await db.FinanceRegulatorySchemas.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.Code == normalized && x.IsActive).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Active regulatory schema was not found.");
        var dataset = await DatasetAsync(tenantId, command.From.ToUniversalTime(), command.To.ToUniversalTime(), ct);
        var definition = JsonSerializer.Deserialize<SchemaDefinition>(schema.DefinitionJson, JsonOptions()) ?? throw new InvalidOperationException("Regulatory schema definition is invalid.");
        var rendered = Render(schema, definition, dataset);
        var signature = await pqc.SignAsync(tenantId, "finance-regulatory-report", rendered.Bytes, archival: false, ct);
        var id = Guid.NewGuid();
        var storageKey = $"finance-regulatory/{tenantId:D}/{schema.Code}/{command.To:yyyy}/{id:N}{rendered.Extension}";
        await storage.UploadAsync(storageKey, new MemoryStream(rendered.Bytes, writable: false), rendered.ContentType, ct);
        var submission = new FinanceRegulatorySubmission { Id = id, TenantId = tenantId, RegulatorySchemaId = schema.Id, PeriodLabel = $"{command.From:O}|{command.To:O}", StorageKey = storageKey, ContentHash = Convert.ToHexString(SHA256.HashData(rendered.Bytes)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = actorId };
        db.FinanceRegulatorySubmissions.Add(submission);
        await db.SaveChangesAsync(ct);
        return submission;
    }

    public async Task<bool> VerifyAsync(Guid tenantId, Guid submissionId, CancellationToken ct)
    {
        var row = await db.FinanceRegulatorySubmissions.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == submissionId, ct);
        if (row is null) return false;
        await using var stream = await storage.DownloadAsync(row.StorageKey, ct);
        if (stream is null) return false;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(row.ContentHash), SHA256.HashData(bytes)) && pqc.Verify(row.PqcAlgorithm, bytes, row.PqcSignature, row.PqcPublicKey);
    }

    private async Task<JsonObject> DatasetAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        var config = await db.TenantFinanceConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        var trial = await finance.GetTrialBalanceAsync(tenantId, from, to, ct);
        var position = await finance.GetFinancialPositionAsync(tenantId, to, ct);
        var profit = await finance.GetProfitLossAsync(tenantId, from, to, ct);
        var withholdings = await db.FinanceWithholdingRecords.AsNoTracking().Where(x => x.TenantId == tenantId && x.WithheldAt >= from && x.WithheldAt <= to).OrderBy(x => x.WithheldAt).ToListAsync(ct);
        return new JsonObject
        {
            ["metadata"] = new JsonObject { ["tenantId"] = tenantId.ToString("D"), ["periodStart"] = from.ToString("O"), ["periodEnd"] = to.ToString("O"), ["currency"] = config?.BaseCurrency ?? "PKR", ["standard"] = position.AccountingStandard },
            ["metrics"] = new JsonObject { ["totalDebit"] = trial.Sum(x => x.Debit), ["totalCredit"] = trial.Sum(x => x.Credit), ["totalAssets"] = position.TotalAssets, ["totalLiabilities"] = position.TotalLiabilities, ["totalEquity"] = position.TotalEquity, ["totalRevenue"] = profit.TotalRevenue, ["totalExpenses"] = profit.TotalExpenses, ["netSurplus"] = profit.NetSurplusOrDeficit, ["netPosition"] = position.TotalAssets - position.TotalLiabilities, ["totalWithheld"] = withholdings.Sum(x => x.WithheldAmount) },
            ["accounts"] = new JsonArray(trial.Select(x => (JsonNode)new JsonObject { ["code"] = x.AccountCode, ["name"] = x.AccountName, ["debit"] = x.Debit, ["credit"] = x.Credit, ["balance"] = x.Balance }).ToArray()),
            ["withholding"] = new JsonArray(withholdings.Select(x => (JsonNode)new JsonObject { ["taxType"] = x.TaxType, ["paymentReference"] = x.PaymentReference, ["grossAmount"] = x.GrossAmount, ["withheldAmount"] = x.WithheldAmount, ["withheldAt"] = x.WithheldAt.ToString("O") }).ToArray())
        };
    }

    private static RenderedReport Render(FinanceRegulatorySchema schema, SchemaDefinition definition, JsonObject dataset)
    {
        var output = Materialize(definition, dataset);
        return schema.Format switch
        {
            "JSON" => new(Encoding.UTF8.GetBytes(output.ToJsonString(JsonOptions())), ".json", "application/json"),
            "CSV" => new(Encoding.UTF8.GetBytes(Csv(output)), ".csv", "text/csv; charset=utf-8"),
            "XML" => new(Xml(schema, output, false), ".xml", "application/xml"),
            "XBRL" => new(Xml(schema, output, true), ".xbrl", "application/xml"),
            "IXBRL" => new(InlineXbrl(schema, output), ".xhtml", "application/xhtml+xml"),
            _ => throw new InvalidOperationException("Regulatory schema format is unsupported.")
        };
    }

    private static JsonObject Materialize(SchemaDefinition definition, JsonObject dataset)
    {
        var output = new JsonObject();
        foreach (var field in definition.Fields)
        {
            XmlConvert.VerifyName(field.Name);
            var value = Resolve(dataset, field.Source);
            if (field.Required && (value is null || string.IsNullOrWhiteSpace(Value(value)))) throw new InvalidOperationException($"Required regulatory source '{field.Source}' has no value.");
            output[field.Name] = value?.DeepClone();
        }
        foreach (var row in definition.Rows)
        {
            XmlConvert.VerifyName(row.Name);
            var source = Resolve(dataset, row.Source) as JsonArray ?? throw new InvalidOperationException($"Regulatory row source '{row.Source}' is not an array.");
            var rows = new JsonArray();
            foreach (var item in source.OfType<JsonObject>())
            {
                var rendered = new JsonObject();
                foreach (var column in row.Columns)
                {
                    XmlConvert.VerifyName(column.Name);
                    rendered[column.Name] = Resolve(item, column.Source)?.DeepClone();
                }
                rows.Add(rendered);
            }
            output[row.Name] = rows;
        }
        return output;
    }

    private static byte[] Xml(FinanceRegulatorySchema schema, JsonObject output, bool xbrl)
    {
        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, OmitXmlDeclaration = false });
        var ns = schema.NamespaceUri ?? string.Empty;
        writer.WriteStartDocument();
        writer.WriteStartElement(xbrl ? "xbrl" : schema.RootElement, xbrl ? "http://www.xbrl.org/2003/instance" : ns);
        if (xbrl) writer.WriteAttributeString("xmlns", "aegis", null, ns);
        WriteObject(writer, output, ns, xbrl ? "aegis" : null);
        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteObject(XmlWriter writer, JsonObject value, string ns, string? prefix)
    {
        foreach (var property in value)
        {
            if (property.Value is JsonArray array)
            {
                writer.WriteStartElement(prefix, property.Key, ns);
                foreach (var item in array.OfType<JsonObject>())
                {
                    writer.WriteStartElement(prefix, "Row", ns);
                    WriteObject(writer, item, ns, prefix);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteElementString(prefix, property.Key, ns, Value(property.Value));
            }
        }
    }

    private static byte[] InlineXbrl(FinanceRegulatorySchema schema, JsonObject output)
    {
        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false });
        writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
        writer.WriteAttributeString("xmlns", "ix", null, "http://www.xbrl.org/2013/inlineXBRL");
        writer.WriteAttributeString("xmlns", "aegis", null, schema.NamespaceUri ?? "urn:aegiscore:regulatory");
        writer.WriteStartElement("body", "http://www.w3.org/1999/xhtml");
        foreach (var property in output.Where(x => x.Value is not JsonArray))
        {
            writer.WriteStartElement("div", "http://www.w3.org/1999/xhtml");
            writer.WriteStartElement("ix", "nonNumeric", "http://www.xbrl.org/2013/inlineXBRL");
            writer.WriteAttributeString("name", $"aegis:{property.Key}");
            writer.WriteString(Value(property.Value));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
        return stream.ToArray();
    }

    private static string Csv(JsonObject output)
    {
        var builder = new StringBuilder();
        var fields = output.Where(x => x.Value is not JsonArray).ToArray();
        builder.AppendLine(string.Join(',', fields.Select(x => Quote(x.Key))));
        builder.AppendLine(string.Join(',', fields.Select(x => Quote(Value(x.Value)))));
        foreach (var section in output.Where(x => x.Value is JsonArray))
        {
            var rows = ((JsonArray)section.Value!).OfType<JsonObject>().ToArray();
            builder.AppendLine();
            builder.AppendLine(Quote(section.Key));
            if (rows.Length == 0) continue;
            var names = rows.SelectMany(x => x.Select(y => y.Key)).Distinct(StringComparer.Ordinal).ToArray();
            builder.AppendLine(string.Join(',', names.Select(Quote)));
            foreach (var row in rows) builder.AppendLine(string.Join(',', names.Select(x => Quote(Value(row[x])))));
        }
        return builder.ToString();
    }

    private async Task SeedAsync(Guid tenantId, CancellationToken ct)
    {
        if (await db.FinanceRegulatorySchemas.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId, ct)) return;
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.EndsWith("regulatory-schemas.json", StringComparison.Ordinal)).Select(Assembly.GetExecutingAssembly().GetManifestResourceStream).FirstOrDefault() ?? throw new InvalidOperationException("Embedded regulatory schemas are missing.");
        var templates = await JsonSerializer.DeserializeAsync<List<SchemaTemplate>>(stream, JsonOptions(), ct) ?? throw new InvalidOperationException("Embedded regulatory schemas are invalid.");
        foreach (var template in templates)
        {
            var definition = template.Definition.ToJsonString(JsonOptions());
            ValidateDefinition(definition);
            db.FinanceRegulatorySchemas.Add(new FinanceRegulatorySchema { Id = Guid.NewGuid(), TenantId = tenantId, Code = template.Code, Jurisdiction = template.Jurisdiction, Version = template.Version, Format = template.Format, RootElement = template.RootElement, NamespaceUri = template.NamespaceUri, DefinitionJson = definition });
        }
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > 256_000) throw new ArgumentException("Regulatory definition is empty or too large.");
        var definition = JsonSerializer.Deserialize<SchemaDefinition>(json, JsonOptions()) ?? throw new ArgumentException("Regulatory definition is invalid.");
        if (definition.Fields.Count + definition.Rows.Sum(x => x.Columns.Count) is 0 or > 500) throw new ArgumentException("Regulatory definition must contain between 1 and 500 mapped fields.");
        foreach (var name in definition.Fields.Select(x => x.Name).Concat(definition.Rows.Select(x => x.Name)).Concat(definition.Rows.SelectMany(x => x.Columns.Select(y => y.Name)))) XmlConvert.VerifyName(name);
        foreach (var path in definition.Fields.Select(x => x.Source).Concat(definition.Rows.Select(x => x.Source)).Concat(definition.Rows.SelectMany(x => x.Columns.Select(y => y.Source))))
            if (string.IsNullOrWhiteSpace(path) || path.Length > 200 || path.Split('.').Any(x => x.Length == 0 || x.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')))) throw new ArgumentException("Regulatory source path is invalid.");
    }

    private static JsonNode? Resolve(JsonNode root, string path)
    {
        JsonNode? current = root;
        foreach (var part in path.Split('.')) current = current?[part];
        return current;
    }

    private static string Value(JsonNode? node) => node is null ? string.Empty : node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node.ToJsonString().Trim('"');
    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string CanonicalJson(string json) => JsonNode.Parse(json)?.ToJsonString(JsonOptions()) ?? throw new ArgumentException("JSON is invalid.");
    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static string Required(string value, int max, string field) { var result = value?.Trim() ?? string.Empty; if (result.Length is 0 || result.Length > max) throw new ArgumentException($"{field} is invalid."); return result; }
    private static string? Optional(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); if (result.Length > max || !Uri.TryCreate(result, UriKind.Absolute, out _)) throw new ArgumentException("Namespace URI is invalid."); return result; }

    private sealed record SchemaDefinition(IReadOnlyList<SchemaField> Fields, IReadOnlyList<SchemaRows> Rows);
    private sealed record SchemaField(string Name, string Source, bool Required = false);
    private sealed record SchemaRows(string Name, string Source, IReadOnlyList<SchemaField> Columns);
    private sealed record SchemaTemplate(string Code, string Jurisdiction, string Version, string Format, string RootElement, string? NamespaceUri, JsonObject Definition);
    private sealed record RenderedReport(byte[] Bytes, string Extension, string ContentType);
}
