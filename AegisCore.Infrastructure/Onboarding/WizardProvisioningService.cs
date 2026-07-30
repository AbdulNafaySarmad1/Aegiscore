using System.Text.Json.Nodes;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardProvisioningService(AegisDbContext db)
{
    public async Task ProvisionAsync(Guid tenantId, string stepData, CancellationToken ct)
    {
        var root = JsonNode.Parse(stepData)?.AsObject() ?? throw new InvalidOperationException("Wizard data is invalid.");
        var university = root["step1"]?.AsObject() ?? throw new InvalidOperationException("University information is missing.");
        var academic = root["step2"]?.AsObject() ?? throw new InvalidOperationException("Academic structure is missing.");
        var finance = root["step4"]?.AsObject() ?? throw new InvalidOperationException("Finance configuration is missing.");
        var fees = root["step5"]?.AsObject() ?? throw new InvalidOperationException("Fee structure is missing.");
        var standard = Required(finance, "standard").ToUpperInvariant();
        var baseCurrency = Required(finance, "baseCurrency").ToUpperInvariant();
        var licenseStep = root["step0"]?.AsObject() ?? throw new InvalidOperationException("License selection is missing.");
        var licenseMode = Required(licenseStep, "mode").ToLowerInvariant();
        if (licenseMode is not ("demo" or "paid")) throw new InvalidOperationException("License mode is invalid.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var campus = await db.Campuses.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.IsMain, ct);
        if (campus is null)
        {
            campus = new AegisCore.Domain.Entities.Campus
            {
                Id = Guid.NewGuid(), TenantId = tenantId, IsMain = true,
                Name = Required(university, "name"), CampusCode = Required(university, "shortName").ToUpperInvariant(),
                Address = Required(university, "street"), City = Required(university, "city"), Country = Required(university, "country"),
                Timezone = Required(university, "timezone"), Language = Required(university, "language")
            };
            db.Campuses.Add(campus);
            db.CampusConfigurations.Add(new CampusConfiguration
            {
                Id = Guid.NewGuid(), CampusId = campus.Id, TenantId = tenantId,
                AcademicCalendarJson = academic.ToJsonString(), FeeStructureJson = finance.ToJsonString(),
                PoliciesJson = new JsonObject { ["contactName"] = university["contactName"]?.GetValue<string>(), ["contactEmail"] = university["contactEmail"]?.GetValue<string>() }.ToJsonString()
            });
        }

        var departments = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in academic["departments"]?.AsArray() ?? [])
        {
            var name = item?["name"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var code = item?["code"]?.GetValue<string>()?.Trim().ToUpperInvariant() ?? name[..Math.Min(8, name.Length)].ToUpperInvariant();
            var department = await db.Departments.SingleOrDefaultAsync(x => x.Code == code || x.Name == name, ct);
            if (department is null)
            {
                department = new Department { Id = Guid.NewGuid(), Name = name, Code = code };
                db.Departments.Add(department);
            }
            departments[code] = department;
        }

        var academicYearName = Required(academic, "academicYear");
        var year = await db.AcademicYears.SingleOrDefaultAsync(x => x.Name == academicYearName, ct);
        if (year is null)
        {
            var start = DateOnly.Parse(Required(academic, "semesterStart"));
            var end = DateOnly.Parse(Required(academic, "semesterEnd"));
            year = new AcademicYear { Id = Guid.NewGuid(), Name = academicYearName, StartDate = start, EndDate = end, IsCurrent = true };
            db.AcademicYears.Add(year);
            db.Semesters.Add(new Semester { Id = Guid.NewGuid(), AcademicYearId = year.Id, Name = academic["term"]?.GetValue<string>() ?? "Current term", Type = academic["calendarType"]?.GetValue<string>() ?? "Semester", StartDate = start, EndDate = end, IsCurrent = true, Status = "active" });
        }

        var fiscalLabel = Required(finance, "fiscalYear");
        if (!await db.FiscalYears.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Name == fiscalLabel, ct))
        {
            var fiscalStartMonth = finance["fiscalYearStartMonth"]?.GetValue<int?>() is { } configuredMonth && configuredMonth is >= 1 and <= 12 ? configuredMonth : 1;
            var fiscalStartYear = DateTime.UtcNow.Month >= fiscalStartMonth ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
            db.FiscalYears.Add(new FiscalYear
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Name = fiscalLabel,
                StartDate = new DateTime(fiscalStartYear, fiscalStartMonth, 1),
                EndDate = new DateTime(fiscalStartYear + 1, fiscalStartMonth, 1).AddDays(-1), IsClosed = false
            });
        }

        var accounts = new (string Code, string Name, AccountType Type)[]
        {
            ("1010", $"Bank - {baseCurrency}", AccountType.Asset),
            ("1100", "Student fee receivable", AccountType.Asset),
            ("2000", "Student deposits payable", AccountType.Liability),
            ("3000", "Retained funds", AccountType.Equity),
            ("4010", $"Tuition revenue - {standard}", AccountType.Revenue),
            ("4020", "Scholarship expense", AccountType.Expense),
            ("5000", "Operating expenses", AccountType.Expense)
        };
        foreach (var account in accounts)
            if (!await db.ChartOfAccounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.AccountCode == account.Code, ct))
                db.ChartOfAccounts.Add(new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = account.Code, AccountName = account.Name, AccountType = account.Type, IsActive = true });

        var programs = new List<DegreeProgram>();
        foreach (var item in academic["programmes"]?.AsArray() ?? [])
        {
            var name = item?["name"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var code = item?["code"]?.GetValue<string>()?.Trim().ToUpperInvariant() ?? name[..Math.Min(12, name.Length)].ToUpperInvariant();
            if (await db.DegreePrograms.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct)) continue;
            var department = departments.Values.FirstOrDefault();
            if (department is null) throw new InvalidOperationException("At least one department is required.");
            var program = new DegreeProgram { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Code = code, DegreeType = item?["degreeLevel"]?.GetValue<string>() ?? "Bachelor", DurationYears = Math.Max(1, (item?["duration"]?.GetValue<int?>() ?? 8) / 2), MinCreditHours = item?["creditHours"]?.GetValue<int?>() ?? 120, DepartmentId = department.Id };
            db.DegreePrograms.Add(program); programs.Add(program);
        }

        var amount = decimal.TryParse(fees["assignmentAmount"]?.GetValue<string>(), out var parsed) ? parsed : 0m;
        foreach (var program in programs)
        {
            if (amount <= 0) continue;
            var structure = new FeeStructure { Id = Guid.NewGuid(), AcademicYearId = year.Id, ProgramId = program.Id, Semester = 1, FeeType = "default", TotalFee = amount };
            structure.Components.Add(new FeeComponent { Id = Guid.NewGuid(), FeeStructureId = structure.Id, ComponentName = "Configured fee", Amount = amount, IsMandatory = true, OrderIndex = 1 });
            db.FeeStructures.Add(structure);
        }
        await db.SaveChangesAsync(ct);
        var license = await db.Licenses.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (license is null)
        {
            var demo = licenseMode == "demo";
            license = new License
            {
                Id = Guid.NewGuid(), TenantId = tenantId, CustomerName = Required(university, "name"),
                CustomerEmail = university["contactEmail"]?.GetValue<string>(),
                LicenseKey = demo ? $"DEMO-{tenantId:N}" : Required(licenseStep, "licenseKey"),
                Edition = demo ? LicenseEdition.Community : LicenseEdition.Professional,
                Status = demo ? LicenseStatus.Trial : LicenseStatus.Active,
                MaxUsers = demo ? 500 : int.MaxValue, MaxCampuses = demo ? 1 : int.MaxValue,
                FeatureEntitlementsJson = new JsonObject { ["mode"] = licenseMode, ["baseCurrency"] = baseCurrency, ["standard"] = standard }.ToJsonString(),
                IssuedAt = DateTime.UtcNow, ExpiresAt = demo ? DateTime.UtcNow.AddDays(30) : null
            };
            db.Licenses.Add(license);
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    private static string Required(JsonObject value, string fieldName) => value[fieldName]?.GetValue<string>()?.Trim() is { Length: > 0 } result ? result : throw new InvalidOperationException($"Required wizard field '{fieldName}' is missing.");
}
