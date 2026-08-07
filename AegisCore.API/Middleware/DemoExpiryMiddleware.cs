using System.Security.Cryptography;
using System.Text;

namespace AegisCore.API.Middleware;

/// <summary>
/// Enforces demo mode expiry. Once a demo expiry is set, it cannot be changed
/// (signed with HMAC to prevent tampering). When expired, the entire application
/// returns a graceful shutdown page. No external URLs, DNS, or CNAMEs — localhost only.
/// </summary>
public sealed class DemoExpiryMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/wizard/activate-demo", "/api/wizard/demo-status", "/api/portal-health",
        "/health", "/health/ready", "/health/live", "/portal-health"
    };

    public DemoExpiryMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo)
        {
            await _next(context);
            return;
        }

        // Check expiry
        var expiryRaw = configuration["Runtime:DemoExpiryUtc"];
        var expirySig = configuration["Runtime:DemoExpirySignature"];
        var hmacKey = configuration["AEGISCORE_ENCRYPTION_KEY"] ?? "demo-hmac-key-fallback";

        if (!string.IsNullOrWhiteSpace(expiryRaw) && !string.IsNullOrWhiteSpace(expirySig))
        {
            // Verify signature — prevents tampering with expiry
            var expectedSig = ComputeHmac(expiryRaw, hmacKey);
            if (!string.Equals(expectedSig, expirySig, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(DemoTamperedPage());
                return;
            }

            if (DateTime.TryParse(expiryRaw, out var expiry) && DateTime.UtcNow >= expiry)
            {
                // Demo expired — graceful shutdown
                if (AllowedPaths.Contains(context.Request.Path.Value ?? ""))
                {
                    await _next(context);
                    return;
                }

                context.Response.StatusCode = 410; // Gone
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(DemoExpiredPage(expiry));
                return;
            }
        }

        // Inject demo badge into every HTML response
        await _next(context);

        if (context.Response.ContentType?.Contains("text/html") == true)
        {
            // Demo banner is handled by the portal frontend via API check
        }
    }

    private static string ComputeHmac(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    /// <summary>
    /// Sign the expiry so it becomes immutable. Returns the signature that must be stored alongside the expiry.
    /// </summary>
    public static string SignExpiry(string expiryUtc, string hmacKey)
        => ComputeHmac(expiryUtc, hmacKey);

    private static string DemoExpiredPage(DateTime expired)
    {
        var time = expired.ToString("yyyy-MM-dd HH:mm:ss UTC");
        return $@"<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>AegisCore Demo Expired</title>
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
body{{font-family:-apple-system,BlinkMacSystemFont,sans-serif;background:#0F172A;color:#fff;display:flex;align-items:center;justify-content:center;min-height:100vh;text-align:center;padding:2rem}}
.card{{background:#1E293B;border-radius:16px;padding:3rem 2rem;max-width:500px;border:1px solid #334155}}
h1{{font-size:1.5rem;margin-bottom:0.75rem;color:#F59E0B}}p{{color:#94A3B8;line-height:1.6;margin-bottom:0.5rem}}
.time{{font-family:monospace;background:#0F172A;padding:0.5rem 1rem;border-radius:8px;display:inline-block;margin:1rem 0;color:#F59E0B;font-weight:700}}
a{{color:#3B82F6;text-decoration:none}}a:hover{{text-decoration:underline}}
</style></head>
<body>
<div class='card'>
<h1>Demo Period Has Ended</h1>
<p>This AegisCore demo instance expired on:</p>
<div class='time'>{time}</div>
<p>To continue using AegisCore, please contact AegisCore Support for a production license.</p>
<p style='margin-top:1rem'><a href='mailto:sales@aegiscore.pk'>sales@aegiscore.pk</a></p>
</div></body></html>";
    }

    private static string DemoTamperedPage()
    {
        return @"<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Invalid Configuration</title>
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
body{{font-family:-apple-system,BlinkMacSystemFont,sans-serif;background:#0F172A;color:#fff;display:flex;align-items:center;justify-content:center;min-height:100vh;text-align:center;padding:2rem}}
.card{{background:#1E293B;border-radius:16px;padding:3rem 2rem;max-width:500px;border:1px solid #EF4444}}h1{{font-size:1.5rem;color:#EF4444}}p{{color:#94A3B8;margin-top:0.75rem}}
</style></head>
<body><div class='card'><h1>Configuration Tampered</h1><p>The demo expiry has been modified. Demo mode requires an unmodified expiry configuration. Please reset your .env file and restart.</p></div></body></html>";
    }
}
