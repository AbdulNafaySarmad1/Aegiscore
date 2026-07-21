using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.I18n;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.I18n;

public static class I18nServiceExtensions
{
    public static IServiceCollection AddInternationalization(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<ITranslationKeyRepository, TranslationKeyRepository>();
        services.TryAddScoped<ITranslationValueRepository, TranslationValueRepository>();
        services.TryAddScoped<ICountryPackRepository, CountryPackRepository>();
        services.TryAddScoped<ITaxSlabRepository, TaxSlabRepository>();
        services.TryAddScoped<ICurrencyRepository, CurrencyRepository>();

        services.TryAddScoped<ITranslationService, TranslationService>();
        services.TryAddScoped<ICountryPackService, CountryPackService>();

        return services;
    }
}
