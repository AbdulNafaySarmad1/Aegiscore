namespace AegisCore.API.Middleware;

/// <summary>
/// Fails closed if the retired automatic administrator identity is configured.
/// </summary>
public sealed class DemoIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IHostEnvironment environment, ILogger<DemoIdentityMiddleware> logger)
    {
        if (configuration.GetValue<bool>("Runtime:DemoMode"))
        {
            if (!environment.IsDevelopment())
            {
                logger.LogCritical("Runtime:DemoMode is enabled outside Development - refusing to inject demo identity");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "configuration_error",
                    message = "Demo mode is permitted only in an isolated Development environment."
                }, context.RequestAborted);
                return;
            }

        }

        if (configuration.GetValue<bool>("Runtime:InjectDemoIdentity"))
        {
            logger.LogCritical("Legacy automatic demo identity injection is disabled");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "configuration_error",
                message = "Automatic demo identity injection is disabled. Use a seeded demo account."
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
