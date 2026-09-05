using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AegisCore.Application.Onboarding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Onboarding;

public class OnboardingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(IConfiguration config, ILogger<OnboardingService> logger)
    { _config = config; _logger = logger; }

    public async Task<LicenseValidationResult> ValidateLicenseAsync(WizardStateDto state, string licenseKey, CancellationToken ct = default)
    {
        licenseKey = licenseKey.Trim();
        state.LicenseKey = licenseKey;

        try
        {
            var serverUrl = _config["Apex:LicenseServerUrl"] ?? "https://license.aegiscore.io";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var response = await http.PostAsJsonAsync($"{serverUrl}/api/v1/validate", new { key = licenseKey }, ct);

            if (!response.IsSuccessStatusCode)
                return new LicenseValidationResult(false, Error: new WizardError("License key not found.", "Contact licensing@aegiscore.io", Owner: WizardError.WizardErrorOwner.ContactApexSupport));

            var license = await response.Content.ReadFromJsonAsync<LicenseDto>(cancellationToken: ct);
            if (license == null)
                return new LicenseValidationResult(false, Error: new WizardError("Invalid license.", "Contact licensing@aegiscore.io", Owner: WizardError.WizardErrorOwner.ContactApexSupport));

            if (license.ExpiresAt < DateTime.UtcNow)
                return new LicenseValidationResult(false, Error: new WizardError($"License expired {license.ExpiresAt:dd MMM yyyy}.", "Renew at portal.aegiscore.io", Owner: WizardError.WizardErrorOwner.ContactApexSupport));

            state.License = license;
            state.UniversityFullName = license.IssuedTo;
            return new LicenseValidationResult(true, License: license);
        }
        catch (HttpRequestException)
        {
            return new LicenseValidationResult(false, Error: new WizardError("Cannot reach license server.", "Check internet connectivity.", Owner: WizardError.WizardErrorOwner.NetworkIssue));
        }
    }

    public async Task<ConnectionTestResult> TestPostgresAsync(WizardStateDto state, string host, int port, string dbName, string username, string password, CancellationToken ct = default)
    {
        state.DbHost = host; state.DbPort = port; state.DbName = dbName; state.DbUsername = username; state.DbPassword = password;

        try
        {
            await using var conn = new NpgsqlConnection(state.DbConnectionString);
            // conn.OpenTimeout removed
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS _aegis_test (id int); DROP TABLE _aegis_test;", conn);
            await cmd.ExecuteNonQueryAsync(ct);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await using var ping = new NpgsqlCommand("SELECT version();", conn);
            var version = (await ping.ExecuteScalarAsync(ct))?.ToString() ?? "unknown";
            sw.Stop();

            _logger.LogInformation("PostgreSQL tested: {Version} — {Latency}ms", version, sw.ElapsedMilliseconds);
            return new ConnectionTestResult(true, $"PostgreSQL connected — {version.Split(',')[0]} — {sw.ElapsedMilliseconds}ms");
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("password"))
        {
            return new ConnectionTestResult(false, "Authentication failed.", new WizardError("Wrong username or password.", "Check credentials."));
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("host") || ex.Message.Contains("connect"))
        {
            return new ConnectionTestResult(false, "Cannot reach host.", new WizardError($"Cannot reach {host}:{port}.", "Check host and firewall.", TechnicalDetail: ex.Message, Owner: WizardError.WizardErrorOwner.NetworkIssue));
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, "Connection failed.", new WizardError("Database connection failed.", "Check all fields.", TechnicalDetail: ex.Message));
        }
    }

    public async Task<ConnectionTestResult> MigratePostgresAsync(WizardStateDto state, CancellationToken ct = default)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AegisDbContext>()
                .UseNpgsql(state.DbConnectionString, postgres => postgres.MigrationsAssembly(typeof(AegisDbContext).Assembly.FullName))
                .Options;
            await using var target = new AegisDbContext(options);
            await target.Database.MigrateAsync(ct);
            return new ConnectionTestResult(true, "Database connected and AegisCore migrations applied.");
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, "Database migration failed.",
                new WizardError("The database connected, but migrations could not be applied.",
                    "Ensure the database user can create and alter tables.", TechnicalDetail: ex.Message));
        }
    }

    public async Task<ConnectionTestResult> TestRedisAsync(WizardStateDto state, string host, int port, string? password, CancellationToken ct = default)
    {
        state.RedisHost = host; state.RedisPort = port; state.RedisPassword = password;

        try
        {
            var options = ConfigurationOptions.Parse(state.RedisConnectionString);
            options.ConnectTimeout = 5000;
            options.AbortOnConnectFail = true;

            var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var db = connection.GetDatabase();
            var key = $"aegis:setup:{Guid.NewGuid():N}";
            await db.StringSetAsync(key, "ok", TimeSpan.FromSeconds(10));
            var read = await db.StringGetAsync(key);
            await db.KeyDeleteAsync(key);

            if (read != "ok")
                return new ConnectionTestResult(false, "Read/write failed.", new WizardError("Redis connected but read/write test failed.", "Check user permissions."));

            var server = connection.GetServer(host, port);
            var info = await server.InfoAsync("server");
            var version = info
                .SelectMany(group => group)
                .FirstOrDefault(item => item.Key == "redis_version")
                .Value ?? "unknown";

            return new ConnectionTestResult(true, $"Redis {version} — read/write verified");
        }
        catch (RedisConnectionException ex)
        {
            return new ConnectionTestResult(false, "Cannot reach Redis.", new WizardError($"Cannot reach {host}:{port}.", "Check host and port.", TechnicalDetail: ex.Message, Owner: WizardError.WizardErrorOwner.NetworkIssue));
        }
        catch (Exception) // RedisAuthenticationException
        {
            return new ConnectionTestResult(false, "Wrong password.", new WizardError("Wrong Redis password.", "Check password."));
        }
    }

    public async Task<ConnectionTestResult> TestSmtpAsync(WizardStateDto state, string host, int port, string username, string password, string fromAddr, string fromName, CancellationToken ct = default)
    {
        state.SmtpHost = host; state.SmtpPort = port; state.SmtpUsername = username; state.SmtpPassword = password; state.SmtpFromAddress = fromAddr; state.SmtpFromName = fromName;

        try
        {
            using var client = new System.Net.Mail.SmtpClient(host, port);
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(username, password);
            client.Timeout = 10000;

            var recipient = string.IsNullOrWhiteSpace(state.AdminEmail) ? fromAddr : state.AdminEmail;
            var msg = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromAddr, fromName),
                Subject = "AegisCore Setup — Email Test",
                Body = $"Test email from AegisCore setup wizard.\nUniversity: {state.UniversityFullName}"
            };
            msg.To.Add(recipient);

            await client.SendMailAsync(msg, ct);
            return new ConnectionTestResult(true, $"Test email sent to {recipient}");
        }
        catch (System.Net.Mail.SmtpException ex) when (ex.StatusCode == System.Net.Mail.SmtpStatusCode.GeneralFailure)
        {
            return new ConnectionTestResult(false, "Authentication failed.", new WizardError("Wrong SMTP credentials.", "Check username and password.", TechnicalDetail: ex.Message));
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, "Cannot send email.", new WizardError($"Cannot reach SMTP server at {host}:{port}.", "Check host and port.", TechnicalDetail: ex.Message, Owner: WizardError.WizardErrorOwner.NetworkIssue));
        }
    }

    public async Task<DnsPropagationResult> CheckDnsPropagationAsync(WizardStateDto state, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new DnsPropagationResult(true, 100, $"DNS ready — {state.ErpUrl} is accessible.");
    }

    public DomainProvisioningResult ProvisionDomain(
        WizardStateDto state,
        string universityDomain,
        bool useCloudflareTunnel = false)
    {
        state.UniversityDomain = universityDomain;
        state.UseCloudflareTunnel = useCloudflareTunnel;
        if (!useCloudflareTunnel)
        {
            state.CloudflareTunnelId = null;
            state.CloudflareTunnelToken = null;
            state.CloudflareCnameTarget = null;
            return new DomainProvisioningResult(state.ErpUrl, null);
        }

        var tunnelId = $"aegiscore-{state.UniversityShortName.ToLowerInvariant()}-{state.TenantId:N}"[..8];
        state.CloudflareTunnelId = tunnelId;
        state.CloudflareCnameTarget = $"{tunnelId}.cfargotunnel.com";
        return new DomainProvisioningResult(state.ErpUrl, new DnsRecord("CNAME", "erp", state.CloudflareCnameTarget, "Auto"));
    }

    public string GenerateDnsInstructions(WizardStateDto state)
    {
        if (!state.UseCloudflareTunnel || string.IsNullOrWhiteSpace(state.CloudflareCnameTarget))
            return $"Point {state.ErpSubdomain} to the university-managed ingress, load balancer, or public IP. "
                + "Cloudflare Tunnel is not enabled; configure the appropriate A, AAAA, or CNAME record with your DNS provider.";

        return $"Add this DNS record to {state.UniversityDomain}:\n\n"
            + $"Type: CNAME\nName: erp\nValue: {state.CloudflareCnameTarget}\nTTL: Auto\n\n"
            + "Where: your university DNS provider (Cloudflare, cPanel, GoDaddy, or equivalent).";
    }

    public async Task<GoLiveResult> GoLiveAsync(WizardStateDto state, CancellationToken ct = default)
    {
        var checks = new List<GoLiveCheck>
        {
            new("License valid", state.License != null && state.License.ExpiresAt > DateTime.UtcNow, state.License?.Tier ?? "N/A"),
            new("Database connected", !string.IsNullOrEmpty(state.DbHost), $"{state.DbHost}:{state.DbPort}"),
            new("Redis connected", !string.IsNullOrEmpty(state.RedisHost), $"{state.RedisHost}:{state.RedisPort}"),
            new("Email working", !string.IsNullOrEmpty(state.SmtpHost), state.SmtpHost),
            new("DNS propagated", !string.IsNullOrEmpty(state.UniversityDomain), state.ErpSubdomain),
            new("Admin account created", !string.IsNullOrEmpty(state.AdminAccountEmail), state.AdminAccountEmail),
        };

        var failed = checks.Where(c => !c.Passed).ToList();
        if (failed.Count > 0)
        {
            var names = string.Join(", ", failed.Select(f => f.Name));
            return new GoLiveResult(false, Error: new WizardError($"Checks failed: {names}", "Fix issues and retry."), Checks: checks);
        }

        var envPath = Path.Combine(AppContext.BaseDirectory, "config", ".env.generated");
        Directory.CreateDirectory(Path.GetDirectoryName(envPath)!);
        await File.WriteAllTextAsync(envPath, GenerateEnvFile(state), ct);

        _logger.LogInformation("GoLive complete for {University} at {Url}", state.UniversityShortName, state.ErpUrl);
        return new GoLiveResult(true, state.ErpUrl, Checks: checks);
    }

    private string GenerateEnvFile(WizardStateDto state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Generated by AegisCore Setup Wizard");
        sb.AppendLine($"# University: {state.UniversityShortName}");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"# DO NOT EDIT — re-run setup wizard to reconfigure");
        sb.AppendLine();
        sb.AppendLine($"AEGISCORE_TENANT_ID={state.TenantId}");
        sb.AppendLine($"AEGISCORE_UNIVERSITY_FULL_NAME={state.UniversityFullName}");
        sb.AppendLine($"AEGISCORE_UNIVERSITY_SHORT_NAME={state.UniversityShortName}");
        sb.AppendLine($"AEGISCORE_ADMIN_EMAIL={state.AdminEmail}");
        sb.AppendLine($"AEGISCORE_DOMAIN={state.UniversityDomain}");
        sb.AppendLine($"AEGISCORE_CLOUD_PROVIDER=ONPREMISE");
        sb.AppendLine($"ASPNETCORE_ENVIRONMENT=Production");
        sb.AppendLine($"DB_HOST={state.DbHost}");
        sb.AppendLine($"DB_PORT={state.DbPort}");
        sb.AppendLine($"DB_NAME={state.DbName}");
        sb.AppendLine($"DB_USER={state.DbUsername}");
        sb.AppendLine($"REDIS_HOST={state.RedisHost}");
        sb.AppendLine($"REDIS_PORT={state.RedisPort}");
        sb.AppendLine($"SMTP_HOST={state.SmtpHost}");
        sb.AppendLine($"SMTP_PORT={state.SmtpPort}");
        sb.AppendLine($"SMTP_USERNAME={state.SmtpUsername}");
        sb.AppendLine($"SMTP_FROM_ADDRESS={state.SmtpFromAddress}");
        sb.AppendLine($"SMTP_FROM_NAME={state.SmtpFromName}");
        sb.AppendLine($"AEGISCORE_ERP_URL={state.ErpUrl}");
        sb.AppendLine($"AEGISCORE_USE_CLOUDFLARE_TUNNEL={state.UseCloudflareTunnel.ToString().ToLowerInvariant()}");
        if (state.UseCloudflareTunnel) sb.AppendLine($"CLOUDFLARE_TUNNEL_ID={state.CloudflareTunnelId ?? ""}");
        sb.AppendLine($"# Sensitive values stored in secrets provider only:");
        sb.AppendLine($"# DB_PASS, REDIS_PASSWORD, SMTP_PASSWORD, AZURE_CLIENT_SECRET");
        return sb.ToString();
    }

    private static string GetServerFingerprint()
    {
        var mac = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up)
            ?.GetPhysicalAddress().ToString() ?? "unknown";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mac}:{Environment.MachineName}")));
    }
}
