using AegisCore.Domain.Entities;

namespace AegisCore.Application.I18n;

public interface ITranslationService
{
    Task<string> TranslateAsync(string key, string languageCode, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetTranslationsAsync(string languageCode, string? module = null, CancellationToken cancellationToken = default);
    Task<TranslationKey> AddKeyAsync(string key, string module, string defaultValue, CancellationToken cancellationToken = default);
    Task<TranslationValue> SetTranslationAsync(Guid keyId, string languageCode, string value, CancellationToken cancellationToken = default);
}

public interface ICountryPackService
{
    Task<CountryPack> InstallPackAsync(string countryCode, CancellationToken cancellationToken = default);
    Task<CountryPack> GetActivePackAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxSlab>> GetTaxSlabsAsync(Guid countryPackId, TaxType? taxType = null, CancellationToken cancellationToken = default);
}
