using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.RuleEngine;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.RuleEngine;

public static class RuleEngineServiceExtensions
{
    public static IServiceCollection AddRuleEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IRuleDefinitionRepository, RuleDefinitionRepository>();
        services.TryAddScoped<IRuleVersionRepository, RuleVersionRepository>();
        services.TryAddScoped<IDecisionTableRepository, DecisionTableRepository>();

        services.TryAddScoped<IRuleEngineService, RuleEngineService>();

        return services;
    }
}
