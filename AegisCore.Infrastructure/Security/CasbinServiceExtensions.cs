using Casbin;
using Casbin.Model;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Security;

public static class CasbinServiceExtensions
{
    public static IServiceCollection AddCasbinAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IEnforcer>(sp =>
        {
            var e = new Enforcer();
            var model = DefaultModel.Create();
            model.LoadModelFromText(@"
[request_definition]
r = sub, obj, act

[policy_definition]
p = sub, obj, act

[policy_effect]
e = some(where (p.eft == allow))

[matchers]
m = r.sub == p.sub && keyMatch(r.obj, p.obj) && regexMatch(r.act, p.act)
");
            e.SetModel(model);

            e.AddPolicy("SuperAdmin", "/*", ".*");
            e.AddPolicy("admin", "/*", ".*");
            e.AddPolicy("anonymous", "/health", "GET");
            e.AddPolicy("anonymous", "/api/auth/login", "POST");
            e.AddPolicy("anonymous", "/api/auth/refresh", "POST");
            e.AddPolicy("anonymous", "/api/wizard", ".*");
            e.AddPolicy("anonymous", "/api/setup", ".*");
            e.AddPolicy("anonymous", "/swagger", "GET");
            e.AddPolicy("anonymous", "/health", ".*");
            e.AddPolicy("anonymous", "/api/session", ".*");
            e.AddPolicy("anonymous", "/api/examination/transcripts/verify", ".*");
            e.AddPolicy("anonymous", "/api/portal-health", "GET");
            e.AddPolicy("anonymous", "/api/license/heartbeat", "POST");

            return e;
        });

        return services;
    }
}
