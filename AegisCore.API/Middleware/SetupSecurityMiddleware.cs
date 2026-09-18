using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace AegisCore.API.Middleware;

public static class SetupBootstrapToken
{
    public const string HeaderName = "X-Setup-Token";

    public static string RequireValid(IConfiguration configuration)
    {
        var token = configuration["AEGISCORE_SETUP_TOKEN"];
        if (string.IsNullOrWhiteSpace(token))
            throw new ApplicationException("AEGISCORE_SETUP_TOKEN is required before setup mode can start.");
        if (!IsStrong(token))
            throw new ApplicationException("AEGISCORE_SETUP_TOKEN must be a cryptographically random value of at least 32 bytes (64 hexadecimal characters or 32 decoded base64 bytes).");
        return token.Trim();
    }

    public static bool IsStrong(string token)
    {
        var value = token.Trim();
        if (value.Length >= 64 && value.All(Uri.IsHexDigit)) return true;
        try { return Convert.FromBase64String(value).Length >= 32; }
        catch (FormatException) { return false; }
    }
}

public sealed class SetupBootstrapTokenMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(SetupBootstrapToken.RequireValid(configuration));

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsSetupApi(context.Request.Path))
        {
            await next(context);
            return;
        }

        var supplied = Encoding.UTF8.GetBytes(context.Request.Headers[SetupBootstrapToken.HeaderName].ToString());
        if (supplied.Length != _expected.Length || !CryptographicOperations.FixedTimeEquals(supplied, _expected))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Invalid setup token. Check AEGISCORE_SETUP_TOKEN."
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static bool IsSetupApi(PathString path)
        => path.StartsWithSegments("/api/wizard") || path.StartsWithSegments("/api/setup");
}

public sealed class SetupRateLimitMiddleware
{
    private const int UnauthenticatedLimit = 10;
    private const int AuthenticatedLimit = 60;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> Attempts = new(StringComparer.Ordinal);
    private readonly RequestDelegate _next;
    private readonly byte[] _expected;

    public SetupRateLimitMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _expected = Encoding.UTF8.GetBytes(SetupBootstrapToken.RequireValid(configuration));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/wizard"))
        {
            await _next(context);
            return;
        }

        var supplied = Encoding.UTF8.GetBytes(context.Request.Headers[SetupBootstrapToken.HeaderName].ToString());
        var authenticated = supplied.Length == _expected.Length && CryptographicOperations.FixedTimeEquals(supplied, _expected);
        var limit = authenticated ? AuthenticatedLimit : UnauthenticatedLimit;
        // The forwarded address, not the socket: behind the portal proxy every operator would
        // otherwise share one bucket of ten attempts a minute.
        var key = $"{(authenticated ? "valid" : "invalid")}:{ClientAddress.Of(context)}";
        var attempts = Attempts.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        var now = DateTimeOffset.UtcNow;
        lock (attempts)
        {
            while (attempts.TryPeek(out var oldest) && now - oldest >= Window) attempts.Dequeue();
            if (attempts.Count < limit)
            {
                attempts.Enqueue(now);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            }
        }

        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            context.Response.Headers.RetryAfter = "60";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Too many setup attempts. Wait 60 seconds."
            }, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
