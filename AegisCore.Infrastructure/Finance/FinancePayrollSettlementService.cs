using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinancePayrollSettlementService(AegisDbContext db, IPqcCryptographyService encryption, IPqcSignatureService signatures, IR2StorageService storage)
{
    public async Task<FinancePayrollBankInstruction> ConfigureInstructionAsync(Guid tenantId, ConfigurePayrollBankInstructionCommand command, CancellationToken ct)
    {
        if (command.EmployeeId == Guid.Empty || command.AccountReference.Trim().Length is < 6 or > 34 || command.RoutingCode.Trim().Length is < 3 or > 30 || command.AccountName.Trim().Length is < 2 or > 200 || command.Currency.Trim().Length != 3) throw new ArgumentException("Payroll settlement instruction is invalid.");
        if (!await db.Employees.AnyAsync(x => x.TenantId == tenantId && x.Id == command.EmployeeId, ct)) throw new InvalidOperationException("Employee was not found.");
        var instruction = await db.FinancePayrollBankInstructions.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.EmployeeId == command.EmployeeId, ct);
        if (instruction is null) { instruction = new FinancePayrollBankInstruction { Id = Guid.NewGuid(), TenantId = tenantId, EmployeeId = command.EmployeeId }; db.FinancePayrollBankInstructions.Add(instruction); }
        var aad = Encoding.UTF8.GetBytes($"{tenantId:D}|{command.EmployeeId:D}|payroll-bank");
        instruction.EncryptedAccountReference = encryption.Encrypt(Encoding.UTF8.GetBytes(command.AccountReference.Trim()), aad);
        instruction.RoutingCode = command.RoutingCode.Trim(); instruction.AccountName = command.AccountName.Trim(); instruction.Currency = command.Currency.Trim().ToUpperInvariant(); instruction.UpdatedAt = DateTime.UtcNow; instruction.IsActive = true;
        await db.SaveChangesAsync(ct);
        return instruction;
    }

    public async Task<FinancePayrollPaymentFile> GeneratePaymentFileAsync(Guid tenantId, Guid payrollPostingId, Guid actorId, GeneratePayrollPaymentFileCommand command, CancellationToken ct)
    {
        var format = command.Format.Trim().ToUpperInvariant();
        if (format is not ("HBL" or "MCB" or "UBL" or "BACS" or "NACHA" or "RAAST")) throw new ArgumentException("Unsupported payroll payment format.");
        var posting = await db.FinancePayrollPostings.Include(x => x.Lines).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == payrollPostingId, ct) ?? throw new InvalidOperationException("Payroll posting was not found.");
        var employeeIds = posting.Lines.Select(x => x.EmployeeId).ToArray();
        var instructions = await db.FinancePayrollBankInstructions.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsActive && employeeIds.Contains(x.EmployeeId)).ToDictionaryAsync(x => x.EmployeeId, ct);
        if (instructions.Count != employeeIds.Distinct().Count()) throw new InvalidOperationException("Every payroll employee must have an active settlement instruction.");
        var rows = posting.Lines.Select(line =>
        {
            var instruction = instructions[line.EmployeeId];
            var aad = Encoding.UTF8.GetBytes($"{tenantId:D}|{line.EmployeeId:D}|payroll-bank");
            var account = Encoding.UTF8.GetString(encryption.Decrypt(instruction.EncryptedAccountReference, aad));
            return new PaymentRow(line.EmployeeId, instruction.AccountName, account, instruction.RoutingCode, instruction.Currency, line.NetSalary);
        }).ToList();
        if (rows.Any(x => x.Currency != rows[0].Currency)) throw new InvalidOperationException("A payment file cannot mix currencies.");
        var payload = format switch { "NACHA" => Nacha(rows, posting.Period), "BACS" => Bacs(rows, posting.Period), "RAAST" => JsonSerializer.Serialize(rows.Select(x => new { employeeId = x.EmployeeId, iban = x.Account, amount = x.Amount, currency = x.Currency, reference = $"PAY-{posting.Period}" })), _ => Delimited(rows, format == "MCB" ? '|' : ',') };
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = SHA256.HashData(bytes); var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var signed = await signatures.SignAsync(tenantId, "payroll-payment-file", hashBytes, false, ct);
        var extension = format == "RAAST" ? "json" : "txt";
        var key = $"tenants/{tenantId:D}/finance/payroll/{posting.Period}/{format.ToLowerInvariant()}-{Guid.CreateVersion7():N}.{extension}";
        await using var stream = new MemoryStream(bytes, false); await storage.UploadAsync(key, stream, format == "RAAST" ? "application/json" : "text/plain", ct);
        var artifact = new FinancePayrollPaymentFile { Id = Guid.NewGuid(), TenantId = tenantId, PayrollPostingId = posting.Id, Format = format, StorageKey = key, Sha256 = hash, PqcAlgorithm = signed.Algorithm, PqcKeyId = signed.KeyId, PqcSignature = signed.Signature, TotalAmount = rows.Sum(x => x.Amount), RecordCount = rows.Count, GeneratedByUserId = actorId };
        db.FinancePayrollPaymentFiles.Add(artifact); await db.SaveChangesAsync(ct); return artifact;
    }

    public async Task<FinanceGratuityAssumption> ConfigureAssumptionAsync(Guid tenantId, ConfigureGratuityAssumptionCommand command, CancellationToken ct)
    {
        if (command.DiscountRatePercent is < 0m or > 100m || command.SalaryGrowthRatePercent is < 0m or > 100m || command.TurnoverRatePercent is < 0m or > 100m || command.BenefitDivisor <= 0m || command.RetirementHorizonYears is < 1 or > 60) throw new ArgumentException("Gratuity assumptions are invalid.");
        var active = await db.FinanceGratuityAssumptions.Where(x => x.TenantId == tenantId && x.EffectiveTo == null).ToListAsync(ct);
        foreach (var old in active) old.EffectiveTo = command.EffectiveFrom.ToUniversalTime().AddTicks(-1);
        var assumption = new FinanceGratuityAssumption { Id = Guid.NewGuid(), TenantId = tenantId, DiscountRatePercent = command.DiscountRatePercent, SalaryGrowthRatePercent = command.SalaryGrowthRatePercent, TurnoverRatePercent = command.TurnoverRatePercent, BenefitDivisor = command.BenefitDivisor, RetirementHorizonYears = command.RetirementHorizonYears, EffectiveFrom = command.EffectiveFrom.ToUniversalTime() };
        db.FinanceGratuityAssumptions.Add(assumption); await db.SaveChangesAsync(ct); return assumption;
    }

    public async Task<FinanceGratuityValuation> RunValuationAsync(Guid tenantId, Guid actorId, DateTime valuationDate, CancellationToken ct)
    {
        valuationDate = valuationDate.ToUniversalTime();
        var assumption = await db.FinanceGratuityAssumptions.AsNoTracking().Where(x => x.TenantId == tenantId && x.EffectiveFrom <= valuationDate && (x.EffectiveTo == null || x.EffectiveTo >= valuationDate)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Gratuity assumptions are not configured.");
        var employees = await db.Employees.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == EmployeeStatus.Active && x.Salary > 0m && x.DateOfJoining != null).ToListAsync(ct);
        decimal obligation = 0m;
        foreach (var employee in employees)
        {
            var serviceYears = Math.Max(0m, (decimal)(valuationDate.Date - employee.DateOfJoining!.Value.Date).Days / 365.25m);
            var projected = employee.Salary!.Value * Power(1m + assumption.SalaryGrowthRatePercent / 100m, assumption.RetirementHorizonYears);
            var benefit = projected / assumption.BenefitDivisor * serviceYears * 30m;
            var survival = Power(1m - assumption.TurnoverRatePercent / 100m, assumption.RetirementHorizonYears);
            obligation += benefit * survival / Power(1m + assumption.DiscountRatePercent / 100m, assumption.RetirementHorizonYears);
        }
        obligation = decimal.Round(obligation, 4, MidpointRounding.AwayFromZero);
        var prior = await db.FinanceGratuityValuations.AsNoTracking().Where(x => x.TenantId == tenantId && x.ValuationDate < valuationDate).OrderByDescending(x => x.ValuationDate).FirstOrDefaultAsync(ct);
        var currentService = decimal.Round(obligation / assumption.RetirementHorizonYears / 12m, 4, MidpointRounding.AwayFromZero);
        var financeCost = decimal.Round((prior?.DefinedBenefitObligation ?? 0m) * assumption.DiscountRatePercent / 100m / 12m, 4, MidpointRounding.AwayFromZero);
        var charge = currentService + financeCost;
        var expense = await AccountAsync(tenantId, "6001", "Gratuity expense", AccountType.Expense, ct); var payable = await AccountAsync(tenantId, "2113", "Gratuity payable", AccountType.Liability, ct);
        var journal = new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"GRAT-{valuationDate:yyyyMM}-{Guid.NewGuid():N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"IAS 19 gratuity valuation {valuationDate:yyyy-MM}", EntryDate = valuationDate, TotalDebit = charge, TotalCredit = charge, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = charge }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = payable.Id, Credit = charge }] };
        var valuation = new FinanceGratuityValuation { Id = Guid.NewGuid(), TenantId = tenantId, AssumptionId = assumption.Id, ValuationDate = valuationDate.Date, DefinedBenefitObligation = obligation, CurrentServiceCost = currentService, InterestCost = financeCost, JournalEntryId = journal.Id };
        db.JournalEntries.Add(journal); db.FinanceGratuityValuations.Add(valuation); await db.SaveChangesAsync(ct); return valuation;
    }

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct) { var account = await db.ChartOfAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.AccountCode == code, ct); if (account is not null) return account; account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = code, AccountName = name, AccountType = type }; db.ChartOfAccounts.Add(account); return account; }
    private static decimal Power(decimal value, int exponent) { var result = 1m; for (var index = 0; index < exponent; index++) result *= value; return result; }
    private static string Delimited(IEnumerable<PaymentRow> rows, char delimiter) => string.Join('\n', rows.Select(x => string.Join(delimiter, x.EmployeeId.ToString("D"), Safe(x.Name, delimiter), x.Account, x.Routing, x.Amount.ToString("0.00", CultureInfo.InvariantCulture), x.Currency)));
    private static string Safe(string value, char delimiter) => value.Replace(delimiter, ' ').Replace('\r', ' ').Replace('\n', ' ');
    private static string Bacs(IEnumerable<PaymentRow> rows, string period) => string.Join('\n', rows.Select(x => $"{x.Routing.PadLeft(6, '0')[..6]}{x.Account.PadLeft(8, '0')[..8]}{x.Amount * 100m:00000000000}{Safe(x.Name, ' ')[..Math.Min(18, x.Name.Length)].PadRight(18)}PAY{period.Replace("-", "", StringComparison.Ordinal)}"));
    private static string Nacha(IReadOnlyList<PaymentRow> rows, string period)
    {
        var header = $"101 000000000 000000000{DateTime.UtcNow:yyMMddHHmm}A094101AEGISCORE BANK         AEGISCORE PAYROLL".PadRight(94);
        var batch = ("5220AEGISCORE PAYROLL                     0000000000PPDPAYROLL   " + period.Replace("-", "", StringComparison.Ordinal).PadRight(12) + "1000000010000001").PadRight(94);
        var output = new List<string> { header[..94], batch[..94] };
        long hash = 0; long total = 0; var sequence = 1;
        foreach (var row in rows) { if (row.Routing.Length != 9 || !row.Routing.All(char.IsDigit)) throw new InvalidOperationException("NACHA routing codes must contain 9 digits."); hash += long.Parse(row.Routing[..8], CultureInfo.InvariantCulture); var cents = checked((long)decimal.Round(row.Amount * 100m, 0)); total += cents; output.Add(($"622{row.Routing}{row.Account.PadRight(17)}{cents:0000000000}{row.EmployeeId:N}{Safe(row.Name, ' ').PadRight(22)}  0000001{sequence:0000000}")[..94]); sequence++; }
        output.Add($"8220{rows.Count:000000}{hash % 10_000_000_000:0000000000}{0:000000000000}{total:000000000000}0000000000                         00000010000001".PadRight(94)[..94]);
        output.Add($"9{1:000000}{Math.Max(1, (output.Count + 9) / 10):000000}{rows.Count:00000000}{hash % 10_000_000_000:0000000000}{0:000000000000}{total:000000000000}".PadRight(94, '9'));
        while (output.Count % 10 != 0) output.Add(new string('9', 94)); return string.Join('\n', output);
    }
    private sealed record PaymentRow(Guid EmployeeId, string Name, string Account, string Routing, string Currency, decimal Amount);
}
