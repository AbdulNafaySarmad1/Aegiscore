using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.I18n;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/i18n")]
public class I18nController : ControllerBase
{
    private readonly ITranslationService _translationService;
    private readonly ITranslationKeyRepository _keyRepo;
    private readonly ILogger<I18nController> _logger;

    public I18nController(
        ITranslationService translationService,
        ITranslationKeyRepository keyRepo,
        ILogger<I18nController> logger)
    {
        _translationService = translationService;
        _keyRepo = keyRepo;
        _logger = logger;
    }

    [HttpGet("translate/{key}")]
    public async Task<IActionResult> Translate(string key, [FromQuery] string lang = "en", CancellationToken cancellationToken = default)
    {
        var value = await _translationService.TranslateAsync(key, lang, cancellationToken);
        return Ok(new { key, language = lang, value });
    }

    [HttpGet("translations")]
    public async Task<IActionResult> GetTranslations([FromQuery] string lang = "en", [FromQuery] string? module = null, CancellationToken cancellationToken = default)
    {
        var translations = await _translationService.GetTranslationsAsync(lang, module, cancellationToken);
        return Ok(new { language = lang, total = translations.Count, translations });
    }

    [HttpPost("keys")]
    public async Task<IActionResult> AddKey([FromBody] SetTranslationRequest request, CancellationToken cancellationToken)
    {
        var key = await _translationService.AddKeyAsync(request.Key, request.Module, request.DefaultValue ?? "", cancellationToken);
        var translation = await _translationService.SetTranslationAsync(key.Id, request.LanguageCode, request.Value, cancellationToken);

        return Ok(new TranslationKeyDto
        {
            Id = key.Id, Key = key.Key, Module = key.Module, DefaultValue = translation.Value,
            Translations = new Dictionary<string, string> { [request.LanguageCode] = request.Value }
        });
    }

    [HttpGet("keys")]
    public async Task<IActionResult> ListKeys([FromQuery] string? module, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.TranslationKey> keys;
        if (!string.IsNullOrWhiteSpace(search))
            keys = await _keyRepo.SearchAsync(search, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(module))
            keys = await _keyRepo.GetByModuleAsync(module, cancellationToken);
        else
            keys = await _keyRepo.GetAllAsync(cancellationToken);

        var response = keys.Select(k => new TranslationKeyDto
        {
            Id = k.Id, Key = k.Key, Module = k.Module,
            DefaultValue = k.Values.FirstOrDefault(v => v.LanguageCode == "en")?.Value ?? "",
            Translations = k.Values.ToDictionary(v => v.LanguageCode, v => v.Value)
        });

        return Ok(new { total = response.Count(), keys = response });
    }
}

[ApiController]
[Route("api/country-packs")]
[AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
public class CountryPackController : ControllerBase
{
    private readonly ICountryPackService _packService;
    private readonly ICountryPackRepository _packRepo;
    private readonly ICurrencyRepository _currencyRepo;
    private readonly ILogger<CountryPackController> _logger;

    public CountryPackController(
        ICountryPackService packService,
        ICountryPackRepository packRepo,
        ICurrencyRepository currencyRepo,
        ILogger<CountryPackController> logger)
    {
        _packService = packService;
        _packRepo = packRepo;
        _currencyRepo = currencyRepo;
        _logger = logger;
    }

    [HttpPost("install/{countryCode}")]
    public async Task<IActionResult> InstallPack(string countryCode, CancellationToken cancellationToken)
    {
        var pack = await _packService.InstallPackAsync(countryCode, cancellationToken);
        return Ok(new CountryPackDto
        {
            Id = pack.Id, Name = pack.Name, CountryCode = pack.CountryCode,
            DefaultLanguage = pack.DefaultLanguage, TextDirection = pack.TextDirection.ToString(),
            DefaultCurrency = pack.DefaultCurrency, Timezone = pack.Timezone,
            CalendarType = pack.DefaultCalendarType.ToString(), GradingScheme = pack.DefaultGradingScheme.ToString(),
            Version = pack.Version, IsInstalled = pack.IsInstalled, InstalledAt = pack.InstalledAt,
            TaxSlabCount = pack.TaxSlabs?.Count ?? 0
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListPacks(CancellationToken cancellationToken)
    {
        var packs = await _packRepo.GetInstalledAsync(cancellationToken);
        var response = packs.Select(p => new CountryPackDto
        {
            Id = p.Id, Name = p.Name, CountryCode = p.CountryCode,
            DefaultLanguage = p.DefaultLanguage, TextDirection = p.TextDirection.ToString(),
            DefaultCurrency = p.DefaultCurrency, Timezone = p.Timezone,
            CalendarType = p.DefaultCalendarType.ToString(), GradingScheme = p.DefaultGradingScheme.ToString(),
            Version = p.Version, IsInstalled = p.IsInstalled, InstalledAt = p.InstalledAt,
            TaxSlabCount = p.TaxSlabs?.Count ?? 0
        });

        return Ok(new { total = response.Count(), packs = response });
    }

    [HttpGet("active/locale")]
    public async Task<IActionResult> GetActiveLocale(CancellationToken cancellationToken)
    {
        var pack = await _packService.GetActivePackAsync(cancellationToken);
        var currency = await _currencyRepo.GetByCodeAsync(pack.DefaultCurrency, cancellationToken);

        return Ok(new LocaleInfo
        {
            LanguageCode = pack.DefaultLanguage,
            TextDirection = pack.TextDirection.ToString(),
            CurrencyCode = pack.DefaultCurrency,
            CurrencySymbol = currency?.Symbol ?? "$",
            Timezone = pack.Timezone ?? "UTC",
            CalendarType = pack.DefaultCalendarType.ToString(),
            GradingScheme = pack.DefaultGradingScheme.ToString()
        });
    }

    [HttpGet("{packId:guid}/tax-slabs")]
    public async Task<IActionResult> GetTaxSlabs(Guid packId, [FromQuery] string? taxType, CancellationToken cancellationToken)
    {
        TaxType? type = null;
        if (!string.IsNullOrWhiteSpace(taxType) && Enum.TryParse<TaxType>(taxType, true, out var parsed))
            type = parsed;

        var slabs = await _packService.GetTaxSlabsAsync(packId, type, cancellationToken);
        var response = slabs.Select(s => new TaxSlabDto
        {
            Id = s.Id, TaxType = s.TaxType.ToString(), Name = s.Name,
            MinAmount = s.MinAmount, MaxAmount = s.MaxAmount, Rate = s.Rate,
            FixedDeduction = s.FixedDeduction, SortOrder = s.SortOrder
        });

        return Ok(new { total = response.Count(), slabs = response });
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> ListCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await _currencyRepo.GetActiveAsync(cancellationToken);
        var response = currencies.Select(c => new CurrencyDto
        {
            Id = c.Id, Code = c.Code, Name = c.Name, Symbol = c.Symbol,
            DecimalPlaces = c.DecimalPlaces, ExchangeRateToBase = c.ExchangeRateToBase, IsBaseCurrency = c.IsBaseCurrency
        });

        return Ok(new { total = response.Count(), currencies = response });
    }
}
