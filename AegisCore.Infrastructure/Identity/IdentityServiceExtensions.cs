using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Identity;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityGovernance(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IPermissionRepository, PermissionRepository>();
        services.TryAddScoped<IUserRoleRepository, UserRoleRepository>();
        services.TryAddScoped<ISamlConfigurationRepository, SamlConfigurationRepository>();

        services.TryAddScoped<IIdentityService, IdentityService>();
        services.TryAddScoped<ISamlService, SamlService>();

        // Sign-in through Entra, Google or Okta: one OpenID Connect handler, configured per tenant.
        services.AddHttpClient(Oidc.OidcClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(20));
        services.TryAddSingleton<Oidc.IOidcClient, Oidc.OidcClient>();
        services.TryAddScoped<IExternalSignInService, ExternalSignInService>();
        services.TryAddSingleton<ISignInHandoffService, SignInHandoffService>();

        return services;
    }
}
