using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.I18n;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.I18n;

public class CountryPackService : ICountryPackService
{
    private readonly ICountryPackRepository _packRepo;
    private readonly ITaxSlabRepository _taxRepo;
    private readonly ILogger<CountryPackService> _logger;

    public CountryPackService(
        ICountryPackRepository packRepo,
        ITaxSlabRepository taxRepo,
        ILogger<CountryPackService> logger)
    {
        _packRepo = packRepo;
        _taxRepo = taxRepo;
        _logger = logger;
    }

    public async Task<CountryPack> InstallPackAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var existing = await _packRepo.GetByCountryCodeAsync(countryCode, cancellationToken);
        if (existing != null)
        {
            existing.IsInstalled = true;
            existing.InstalledAt = DateTime.UtcNow;
            await _packRepo.UpdateAsync(existing, cancellationToken);
            return existing;
        }

        var pack = CreatePack(countryCode);
        await _packRepo.AddAsync(pack, cancellationToken);

        foreach (var slab in pack.TaxSlabs)
            await _taxRepo.AddAsync(slab, cancellationToken);

        _logger.LogInformation("Country pack installed: {Name} ({Code})", pack.Name, countryCode);
        return pack;
    }

    public async Task<CountryPack> GetActivePackAsync(CancellationToken cancellationToken = default)
    {
        var packs = await _packRepo.GetInstalledAsync(cancellationToken);
        return packs.FirstOrDefault() ?? CreatePack("PK");
    }

    public async Task<IReadOnlyList<TaxSlab>> GetTaxSlabsAsync(Guid countryPackId, TaxType? taxType = null, CancellationToken cancellationToken = default)
    {
        if (taxType.HasValue)
            return await _taxRepo.GetByTypeAsync(countryPackId, taxType.Value, cancellationToken);
        return await _taxRepo.GetByPackAsync(countryPackId, cancellationToken);
    }

    private static CountryPack CreatePack(string countryCode)
    {
        return countryCode.ToUpperInvariant() switch
        {
            "PK" => CreatePakistanPack(),
            "AE" => CreateUAEPack(),
            "SA" => CreateSaudiPack(),
            "MY" => CreateMalaysiaPack(),
            "GB" => CreateUKPack(),
            "US" => CreateUSPack(),
            _ => CreatePakistanPack()
        };
    }

    private static CountryPack CreatePakistanPack()
    {
        var pack = new CountryPack
        {
            Id = Guid.NewGuid(), Name = "Pakistan Pack", CountryCode = "PK",
            DefaultLanguage = "ur", TextDirection = TextDirection.RTL, DefaultCurrency = "PKR",
            Timezone = "Asia/Karachi", DefaultCalendarType = CalendarType.Semester,
            DefaultGradingScheme = GradingScheme.GPA4, Version = "1.0.0",
            Status = CountryPackStatus.Active, IsInstalled = true
        };

        pack.TaxSlabs = new List<TaxSlab>
        {
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.IncomeTax, Name = "FBR 0–600K", MinAmount = 0, MaxAmount = 600000, Rate = 0, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.IncomeTax, Name = "FBR 600K–1.2M", MinAmount = 600001, MaxAmount = 1200000, Rate = 0.025m, FixedDeduction = 0, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.IncomeTax, Name = "FBR 1.2M–2.4M", MinAmount = 1200001, MaxAmount = 2400000, Rate = 0.125m, FixedDeduction = 15000, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.IncomeTax, Name = "FBR 2.4M–3.6M", MinAmount = 2400001, MaxAmount = 3600000, Rate = 0.20m, FixedDeduction = 165000, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.IncomeTax, Name = "FBR Above 3.6M", MinAmount = 3600001, MaxAmount = null, Rate = 0.25m, FixedDeduction = 405000, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.SalesTax, Name = "GST Standard Rate", MinAmount = 0, MaxAmount = null, Rate = 0.18m, SortOrder = 6 }
        };

        return pack;
    }

    private static CountryPack CreateUAEPack()
    {
        var pack = new CountryPack
        {
            Id = Guid.NewGuid(), Name = "UAE Pack", CountryCode = "AE",
            DefaultLanguage = "ar", TextDirection = TextDirection.RTL, DefaultCurrency = "AED",
            Timezone = "Asia/Dubai", DefaultCalendarType = CalendarType.Semester,
            DefaultGradingScheme = GradingScheme.GPA4, Version = "1.0.0"
        };

        pack.TaxSlabs = new List<TaxSlab>
        {
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.VAT, Name = "VAT Standard Rate", MinAmount = 0, MaxAmount = null, Rate = 0.05m, SortOrder = 1 }
        };

        return pack;
    }

    private static CountryPack CreateSaudiPack()
    {
        var pack = new CountryPack
        {
            Id = Guid.NewGuid(), Name = "Saudi Arabia Pack", CountryCode = "SA",
            DefaultLanguage = "ar", TextDirection = TextDirection.RTL, DefaultCurrency = "SAR",
            Timezone = "Asia/Riyadh", DefaultCalendarType = CalendarType.Semester,
            DefaultGradingScheme = GradingScheme.GPA5, Version = "1.0.0"
        };

        pack.TaxSlabs = new List<TaxSlab>
        {
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.VAT, Name = "VAT Standard Rate", MinAmount = 0, MaxAmount = null, Rate = 0.15m, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.Zakat, Name = "Zakat Deduction", MinAmount = 0, MaxAmount = null, Rate = 0.025m, SortOrder = 2 }
        };

        return pack;
    }

    private static CountryPack CreateMalaysiaPack()
    {
        var pack = new CountryPack
        {
            Id = Guid.NewGuid(), Name = "Malaysia Pack", CountryCode = "MY",
            DefaultLanguage = "ms", TextDirection = TextDirection.LTR, DefaultCurrency = "MYR",
            Timezone = "Asia/Kuala_Lumpur", DefaultCalendarType = CalendarType.Semester,
            DefaultGradingScheme = GradingScheme.GPA4, Version = "1.0.0"
        };

        pack.TaxSlabs = new List<TaxSlab>
        {
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.SST, Name = "SST Service Tax", MinAmount = 0, MaxAmount = null, Rate = 0.06m, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), CountryPackId = pack.Id, TaxType = TaxType.SST, Name = "SST Sales Tax", MinAmount = 0, MaxAmount = null, Rate = 0.10m, SortOrder = 2 }
        };

        return pack;
    }

    private static CountryPack CreateUKPack()
    {
        return new CountryPack
        {
            Id = Guid.NewGuid(), Name = "UK Pack", CountryCode = "GB",
            DefaultLanguage = "en", TextDirection = TextDirection.LTR, DefaultCurrency = "GBP",
            Timezone = "Europe/London", DefaultCalendarType = CalendarType.Trimester,
            DefaultGradingScheme = GradingScheme.Percentage, Version = "1.0.0"
        };
    }

    private static CountryPack CreateUSPack()
    {
        return new CountryPack
        {
            Id = Guid.NewGuid(), Name = "US Pack", CountryCode = "US",
            DefaultLanguage = "en", TextDirection = TextDirection.LTR, DefaultCurrency = "USD",
            Timezone = "America/New_York", DefaultCalendarType = CalendarType.Semester,
            DefaultGradingScheme = GradingScheme.GPA4, Version = "1.0.0"
        };
    }
}
