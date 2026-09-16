using System.Data;
using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class AncillaryFinanceService(AegisDbContext db, IFinanceModuleService finance, FinanceAssetLifecycleService assets)
{
    public async Task<FinanceLibraryAcquisition> CreateLibraryAcquisitionAsync(Guid tenantId, Guid actorId, CreateFinanceLibraryAcquisitionCommand command, CancellationToken ct)
    {
        var currency = Currency(command.Currency);
        var resourceType = Required(command.ResourceType, 20, "Resource type").ToUpperInvariant();
        if (actorId == Guid.Empty || command.Cost < 0m || command.FairValue < 0m || command.UsefulLifeMonths < 0 || command.AcquiredAt == default || command.SubscriptionEndsAt <= command.AcquiredAt)
            throw new ArgumentException("Library acquisition fields are invalid.");
        var amount = command.IsDonation ? command.FairValue : command.Cost;
        if (amount <= 0m) throw new ArgumentException("Library acquisition value must be positive.");
        var config = await db.TenantFinanceConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        var threshold = config?.LibraryCapitalizationThreshold ?? 5000m;
        var physicalCapitalized = resourceType == "PHYSICAL" && command.Cost >= threshold && command.UsefulLifeMonths > 12;
        var prepaid = resourceType == "DIGITAL" && command.SubscriptionEndsAt is not null;
        var treatment = command.IsDonation || physicalCapitalized ? "CAPITALIZED" : prepaid ? "PREPAID" : "EXPENSE";
        var debit = await AccountAsync(tenantId, treatment switch { "CAPITALIZED" => "1520", "PREPAID" => "1410", _ => "6320" }, treatment switch { "CAPITALIZED" => "Library collection asset", "PREPAID" => "Prepaid digital resources", _ => "Library materials expense" }, treatment == "EXPENSE" ? AccountType.Expense : AccountType.Asset, ct);
        var credit = await AccountAsync(tenantId, command.IsDonation ? "4300" : "2100", command.IsDonation ? "Donated asset income" : "Vendor payable", command.IsDonation ? AccountType.Revenue : AccountType.Liability, ct);
        var acquisition = new FinanceLibraryAcquisition
        {
            Id = Guid.NewGuid(), TenantId = tenantId, LibraryBookId = command.LibraryBookId, ResourceType = resourceType,
            Title = Required(command.Title, 300, "Title"), Currency = currency, Cost = command.Cost, FairValue = amount,
            UsefulLifeMonths = command.UsefulLifeMonths, AcquiredAt = command.AcquiredAt.ToUniversalTime(), SubscriptionEndsAt = command.SubscriptionEndsAt?.ToUniversalTime(), AccountingTreatment = treatment
        };
        var journal = Journal(tenantId, actorId, $"LIB-ACQ-{acquisition.Id:N}", $"Library acquisition: {acquisition.Title}", amount, debit.Id, credit.Id);
        acquisition.JournalEntryId = journal.Id;
        db.FinanceLibraryAcquisitions.Add(acquisition);
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return acquisition;
    }

    public async Task<int> RunLibraryAmortizationAsync(Guid tenantId, Guid actorId, RunLibraryAmortizationCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || command.ThroughDate == default) throw new ArgumentException("Amortization date and actor are required.");
        var through = command.ThroughDate.ToUniversalTime();
        var rows = await db.FinanceLibraryAcquisitions.Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" && (x.AccountingTreatment == "CAPITALIZED" || x.AccountingTreatment == "PREPAID") && x.AcquiredAt <= through).ToListAsync(ct);
        var expense = await AccountAsync(tenantId, "6321", "Library depreciation and amortization", AccountType.Expense, ct);
        var accumulated = await AccountAsync(tenantId, "1595", "Accumulated library depreciation and amortization", AccountType.Asset, ct);
        var posted = 0;
        foreach (var row in rows)
        {
            var end = row.SubscriptionEndsAt ?? (row.UsefulLifeMonths > 0 ? row.AcquiredAt.AddMonths(row.UsefulLifeMonths) : row.AcquiredAt);
            if (end <= row.AcquiredAt) continue;
            var effective = through < end ? through : end;
            var elapsedTicks = Math.Max(0L, effective.Ticks - row.AcquiredAt.Ticks);
            var totalTicks = end.Ticks - row.AcquiredAt.Ticks;
            var depreciable = row.AccountingTreatment == "CAPITALIZED" && row.ResourceType == "PHYSICAL" ? Math.Max(0m, row.FairValue - 1m) : row.FairValue;
            var target = decimal.Round(depreciable * elapsedTicks / totalTicks, 4, MidpointRounding.ToEven);
            var charge = target - row.AccumulatedAmortization;
            if (charge <= 0m) continue;
            var period = $"{through:yyyyMM}";
            var number = $"LIB-AM-{row.Id:N}-{period}";
            if (await db.JournalEntries.AnyAsync(x => x.TenantId == tenantId && x.EntryNumber == number, ct)) continue;
            db.JournalEntries.Add(Journal(tenantId, actorId, number, $"Library amortization: {row.Title}", charge, expense.Id, accumulated.Id));
            row.AccumulatedAmortization = target;
            posted++;
        }
        await db.SaveChangesAsync(ct);
        return posted;
    }

    public async Task<FinanceLibraryFine> CreateLibraryFineAsync(Guid tenantId, CreateFinanceLibraryFineCommand command, CancellationToken ct)
    {
        if (command.StudentId == Guid.Empty || command.Amount <= 0m || !await db.StudentProfiles.AnyAsync(x => x.TenantId == tenantId && x.StudentId == command.StudentId, ct))
            throw new ArgumentException("Tenant student and a positive fine amount are required.");
        var fine = new FinanceLibraryFine { Id = Guid.NewGuid(), TenantId = tenantId, StudentId = command.StudentId, LibraryBookId = command.LibraryBookId, FineType = Required(command.FineType, 30, "Fine type").ToUpperInvariant(), Amount = decimal.Round(command.Amount, 4, MidpointRounding.AwayFromZero), Currency = Currency(command.Currency), Description = Required(command.Description, 500, "Description") };
        db.FinanceLibraryFines.Add(fine);
        await db.SaveChangesAsync(ct);
        return fine;
    }

    public async Task<int> PostOpenLibraryFinesAsync(Guid tenantId, Guid actorId, CancellationToken ct)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Posting actor is required.");
        var fines = await db.FinanceLibraryFines.Where(x => x.TenantId == tenantId && x.Status == "OPEN" && x.JournalEntryId == null).ToListAsync(ct);
        if (fines.Count == 0) return 0;
        var receivable = await AccountAsync(tenantId, "1100", "Student fee receivable", AccountType.Asset, ct);
        var income = await AccountAsync(tenantId, "4320", "Library fine income", AccountType.Revenue, ct);
        foreach (var fine in fines)
        {
            var journal = Journal(tenantId, actorId, $"LIB-FINE-{fine.Id:N}", fine.Description, fine.Amount, receivable.Id, income.Id);
            fine.JournalEntryId = journal.Id;
            fine.Status = "POSTED";
            db.JournalEntries.Add(journal);
        }
        await db.SaveChangesAsync(ct);
        return fines.Count;
    }

    public async Task<FinanceFleetProfile> RegisterFleetAsync(Guid tenantId, Guid actorId, RegisterFinanceFleetProfileCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || command.VehicleId == Guid.Empty || command.CostCenterId == Guid.Empty || command.AcquisitionCost <= 0m || command.UsefulLifeYears <= 0 || command.ServiceIntervalDays <= 0 || command.ServiceIntervalKm <= 0m || command.OpeningOdometer < 0m || command.ModelYear is < 1900 or > 2200)
            throw new ArgumentException("Fleet profile fields are invalid.");
        var vehicle = await db.TransportVehicles.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.VehicleId, ct) ?? throw new KeyNotFoundException("Tenant vehicle was not found.");
        if (await db.FinanceFleetProfiles.AnyAsync(x => x.TenantId == tenantId && x.TransportVehicleId == command.VehicleId, ct)) throw new InvalidOperationException("Vehicle already has a finance profile.");
        var category = await db.AssetCategories.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "VEH");
        if (category is null)
        {
            category = new AssetCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Vehicles", Code = "VEH" };
            db.AssetCategories.Add(category);
        }
        var asset = new Asset { Id = Guid.NewGuid(), TenantId = tenantId, Name = vehicle.RegistrationNumber, SerialNumber = vehicle.RegistrationNumber, AssetCategoryId = category.Id, Status = AssetStatus.Active, PurchaseCost = command.AcquisitionCost, CurrentValue = command.AcquisitionCost, PurchaseDate = command.AcquisitionDate.ToUniversalTime(), UsefulLifeYears = command.UsefulLifeYears, DepreciationMethod = DepreciationMethod.ReducingBalance };
        var profile = new FinanceFleetProfile { Id = Guid.NewGuid(), TenantId = tenantId, TransportVehicleId = vehicle.Id, AssetId = asset.Id, CostCenterId = command.CostCenterId, CustodianUserId = command.CustodianUserId, Make = Required(command.Make, 80, "Make"), ModelYear = command.ModelYear, FuelType = Required(command.FuelType, 20, "Fuel type").ToUpperInvariant(), ServiceIntervalDays = command.ServiceIntervalDays, ServiceIntervalKm = command.ServiceIntervalKm, CurrentOdometer = command.OpeningOdometer, NextServiceAt = command.AcquisitionDate.ToUniversalTime().AddDays(command.ServiceIntervalDays) };
        var vehicleAsset = await AccountAsync(tenantId, "1510", "Vehicle assets", AccountType.Asset, ct);
        var payable = await AccountAsync(tenantId, "2100", "Vendor payable", AccountType.Liability, ct);
        db.Assets.Add(asset);
        db.FinanceFleetProfiles.Add(profile);
        db.JournalEntries.Add(Journal(tenantId, actorId, $"FLEET-CAP-{profile.Id:N}", $"Vehicle capitalization: {vehicle.RegistrationNumber}", command.AcquisitionCost, vehicleAsset.Id, payable.Id));
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<VehicleMaintenance> ScheduleMaintenanceAsync(Guid tenantId, Guid vehicleId, ScheduleFleetMaintenanceCommand command, CancellationToken ct)
    {
        if (command.ScheduledDate == default || command.ScheduledDate < DateTime.UtcNow.AddDays(-1)) throw new ArgumentException("Maintenance date is invalid.");
        if (!await db.FinanceFleetProfiles.AnyAsync(x => x.TenantId == tenantId && x.TransportVehicleId == vehicleId && x.Status == "IN_SERVICE", ct)) throw new KeyNotFoundException("Active fleet profile was not found.");
        var row = new VehicleMaintenance { Id = Guid.NewGuid(), TenantId = tenantId, TransportVehicleId = vehicleId, Status = VehicleMaintenanceStatus.Reported, Description = Required(command.Description, 500, "Description"), ScheduledDate = command.ScheduledDate.ToUniversalTime() };
        db.VehicleMaintenances.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<FinanceFleetCostPosting> CompleteMaintenanceAsync(Guid tenantId, Guid actorId, Guid maintenanceId, CompleteFleetMaintenanceCommand command, CancellationToken ct)
    {
        if (command.Cost <= 0m || command.Odometer < 0m) throw new ArgumentException("Maintenance cost and odometer are invalid.");
        var maintenance = await db.VehicleMaintenances.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == maintenanceId, ct) ?? throw new KeyNotFoundException("Maintenance job was not found.");
        var profile = await db.FinanceFleetProfiles.SingleAsync(x => x.TenantId == tenantId && x.TransportVehicleId == maintenance.TransportVehicleId, ct);
        if (maintenance.Status is VehicleMaintenanceStatus.Completed or VehicleMaintenanceStatus.Cancelled || command.Odometer < profile.CurrentOdometer) throw new InvalidOperationException("Maintenance cannot be completed or odometer moved backwards.");
        FinanceFleetCostPosting posting;
        if (command.Capitalize)
        {
            var asset = await db.Assets.SingleAsync(x => x.TenantId == tenantId && x.Id == profile.AssetId, ct);
            var assetAccount = await AccountAsync(tenantId, "1510", "Vehicle assets", AccountType.Asset, ct);
            var payable = await AccountAsync(tenantId, "2105", "Fleet payable", AccountType.Liability, ct);
            var journal = Journal(tenantId, actorId, $"FLEET-OH-{maintenance.Id:N}", command.Description, command.Cost, assetAccount.Id, payable.Id);
            asset.PurchaseCost += command.Cost;
            asset.CurrentValue += command.Cost;
            posting = new FinanceFleetCostPosting { Id = Guid.NewGuid(), TenantId = tenantId, TransportVehicleId = profile.TransportVehicleId, CostCenterId = profile.CostCenterId, CostType = "CAPITAL_OVERHAUL", Amount = command.Cost, Odometer = command.Odometer, Description = Required(command.Description, 500, "Description"), CostDate = DateTime.UtcNow, JournalEntryId = journal.Id };
            db.FinanceFleetCostPostings.Add(posting);
            db.JournalEntries.Add(journal);
        }
        else
        {
            posting = await finance.PostFleetCostAsync(tenantId, actorId, new PostFinanceFleetCostCommand(profile.TransportVehicleId, profile.CostCenterId, "MAINTENANCE", command.Cost, 0m, command.Odometer, Required(command.Description, 500, "Description"), DateTime.UtcNow), ct);
        }
        maintenance.Status = VehicleMaintenanceStatus.Completed;
        maintenance.Cost = command.Cost;
        maintenance.CompletedAt = DateTime.UtcNow;
        profile.CurrentOdometer = command.Odometer;
        profile.LastServiceAt = DateTime.UtcNow;
        profile.NextServiceAt = DateTime.UtcNow.AddDays(profile.ServiceIntervalDays);
        await db.SaveChangesAsync(ct);
        return posting;
    }

    public async Task<FinanceAssetAccountingEvent> DisposeFleetAsync(Guid tenantId, Guid actorId, Guid vehicleId, DisposeFinanceAssetCommand command, CancellationToken ct)
    {
        var profile = await db.FinanceFleetProfiles.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.TransportVehicleId == vehicleId && x.Status != "DISPOSED", ct) ?? throw new KeyNotFoundException("Active fleet profile was not found.");
        var result = await assets.DisposeAsync(tenantId, actorId, profile.AssetId, command, ct);
        profile.Status = "DISPOSED";
        var vehicle = await db.TransportVehicles.SingleAsync(x => x.TenantId == tenantId && x.Id == vehicleId, ct);
        vehicle.Status = VehicleStatus.Retired;
        await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.AccountCode == code, ct);
        if (account is not null) return account;
        account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = code, AccountName = name, AccountType = type };
        db.ChartOfAccounts.Add(account);
        return account;
    }

    private static JournalEntry Journal(Guid tenantId, Guid actorId, string number, string description, decimal amount, Guid debitId, Guid creditId)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = number[..Math.Min(50, number.Length)], EntryType = JournalEntryType.AutoGenerated, Description = description, EntryDate = DateTime.UtcNow, TotalDebit = amount, TotalCredit = amount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = debitId, Debit = amount }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = creditId, Credit = amount }] };

    private static string Currency(string value)
    {
        var result = value.Trim().ToUpperInvariant();
        if (result.Length != 3 || result.Any(c => c is < 'A' or > 'Z')) throw new ArgumentException("Currency must be an ISO 4217 code.");
        return result;
    }

    private static string Required(string value, int max, string field)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length is 0 || result.Length > max) throw new ArgumentException($"{field} is invalid.");
        return result;
    }
}
