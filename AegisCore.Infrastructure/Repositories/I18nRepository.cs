using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class TranslationKeyRepository : Repository<TranslationKey>, ITranslationKeyRepository
{
    public TranslationKeyRepository(AegisDbContext context) : base(context) { }

    public async Task<TranslationKey?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(k => k.Values).FirstOrDefaultAsync(k => k.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<TranslationKey>> GetByModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(k => k.Values).Where(k => k.Module == module).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TranslationKey>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lower = query.ToLowerInvariant();
        return await _dbSet.Include(k => k.Values).Where(k => k.Key.ToLower().Contains(lower) || k.Description!.ToLower().Contains(lower)).ToListAsync(cancellationToken);
    }
}

public class TranslationValueRepository : Repository<TranslationValue>, ITranslationValueRepository
{
    public TranslationValueRepository(AegisDbContext context) : base(context) { }

    public async Task<TranslationValue?> GetByKeyAndLanguageAsync(Guid keyId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.TranslationKeyId == keyId && v.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<IReadOnlyList<TranslationValue>> GetByLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.LanguageCode == languageCode).ToListAsync(cancellationToken);
    }
}

public class CountryPackRepository : Repository<CountryPack>, ICountryPackRepository
{
    public CountryPackRepository(AegisDbContext context) : base(context) { }

    public async Task<CountryPack?> GetByCountryCodeAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(p => p.TaxSlabs).FirstOrDefaultAsync(p => p.CountryCode == countryCode, cancellationToken);
    }

    public async Task<IReadOnlyList<CountryPack>> GetInstalledAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(p => p.TaxSlabs).Where(p => p.IsInstalled).ToListAsync(cancellationToken);
    }
}

public class TaxSlabRepository : Repository<TaxSlab>, ITaxSlabRepository
{
    public TaxSlabRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TaxSlab>> GetByPackAsync(Guid countryPackId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.CountryPackId == countryPackId).OrderBy(t => t.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxSlab>> GetByTypeAsync(Guid countryPackId, TaxType taxType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.CountryPackId == countryPackId && t.TaxType == taxType).OrderBy(t => t.SortOrder).ToListAsync(cancellationToken);
    }
}

public class CurrencyRepository : Repository<Currency>, ICurrencyRepository
{
    public CurrencyRepository(AegisDbContext context) : base(context) { }

    public async Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.IsActive).ToListAsync(cancellationToken);
    }
}
