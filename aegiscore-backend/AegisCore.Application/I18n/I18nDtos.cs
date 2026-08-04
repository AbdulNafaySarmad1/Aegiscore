namespace AegisCore.Application.I18n;

public class TranslationKeyDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public Dictionary<string, string> Translations { get; set; } = new();
}

public class SetTranslationRequest
{
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

public class CountryPackDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = string.Empty;
    public string TextDirection { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public string? Timezone { get; set; }
    public string CalendarType { get; set; } = string.Empty;
    public string GradingScheme { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public DateTime InstalledAt { get; set; }
    public int TaxSlabCount { get; set; }
}

public class TaxSlabDto
{
    public Guid Id { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal? FixedDeduction { get; set; }
    public int SortOrder { get; set; }
}

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public decimal ExchangeRateToBase { get; set; }
    public bool IsBaseCurrency { get; set; }
}

public class LocaleInfo
{
    public string LanguageCode { get; set; } = "en";
    public string TextDirection { get; set; } = "LTR";
    public string CurrencyCode { get; set; } = "USD";
    public string CurrencySymbol { get; set; } = "$";
    public string Timezone { get; set; } = "UTC";
    public string CalendarType { get; set; } = "Semester";
    public string GradingScheme { get; set; } = "GPA4";
}
