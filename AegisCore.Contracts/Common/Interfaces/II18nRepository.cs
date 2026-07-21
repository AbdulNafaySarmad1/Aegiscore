using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ITranslationKeyRepository : IRepository<TranslationKey>
{
    Task<TranslationKey?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationKey>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationKey>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public interface ITranslationValueRepository : IRepository<TranslationValue>
{
    Task<TranslationValue?> GetByKeyAndLanguageAsync(Guid keyId, string languageCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationValue>> GetByLanguageAsync(string languageCode, CancellationToken cancellationToken = default);
}

public interface ICountryPackRepository : IRepository<CountryPack>
{
    Task<CountryPack?> GetByCountryCodeAsync(string countryCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CountryPack>> GetInstalledAsync(CancellationToken cancellationToken = default);
}

public interface ITaxSlabRepository : IRepository<TaxSlab>
{
    Task<IReadOnlyList<TaxSlab>> GetByPackAsync(Guid countryPackId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxSlab>> GetByTypeAsync(Guid countryPackId, TaxType taxType, CancellationToken cancellationToken = default);
}

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default);
}
