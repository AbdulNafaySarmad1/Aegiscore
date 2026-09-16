using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.AspNetCore.Authorization;

namespace AegisCore.API.Extensions;

/// <summary>
/// Once the installation is complete the wizard controller is not mapped, but two things still
/// address its routes: the portal shell polls demo-status on every page, and an operator who
/// bookmarks the setup page deserves a sentence, not a 500.
/// </summary>
public static class SealedWizardEndpoints
{
    public const string SealedMessage = "Installation is complete and the setup wizard is sealed. Configure the institution from the admin console; to reinstall, remove the installation marker from the config volume and restart.";

    public static IEndpointRouteBuilder MapSealedWizard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/wizard/demo-status", async (IConfiguration configuration, ISecretService secrets)
            => Results.Ok(await DemoStatusAsync(configuration, secrets))).AllowAnonymous();
        endpoints.Map("/api/wizard/{**rest}", () => Results.Json(new
        {
            error = "installation_sealed",
            message = SealedMessage
        }, statusCode: StatusCodes.Status410Gone)).AllowAnonymous();
        endpoints.Map("/api/setup/{**rest}", () => Results.Json(new
        {
            error = "installation_sealed",
            message = SealedMessage
        }, statusCode: StatusCodes.Status410Gone)).AllowAnonymous();
        return endpoints;
    }

    public static async Task<object> DemoStatusAsync(IConfiguration configuration, ISecretService secrets)
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo) return new { isDemo = false };

        var expiry = configuration["Runtime:DemoExpiryUtc"];
        if (string.IsNullOrWhiteSpace(expiry))
        {
            try { expiry = await secrets.GetSecretAsync("Runtime__DemoExpiryUtc"); }
            catch { /* secret not set yet — no demo expiry configured */ }
        }
        TimeSpan? remaining = null;
        if (DateTime.TryParse(expiry, out var exp))
            remaining = exp - DateTime.UtcNow;

        return new
        {
            isDemo = true,
            expiryUtc = expiry,
            remainingDays = remaining?.TotalDays ?? -1,
            remainingHours = remaining?.TotalHours ?? -1,
            isExpired = remaining.HasValue && remaining.Value.TotalSeconds <= 0
        };
    }
}
