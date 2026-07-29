using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly WizardStateService _state;
    private readonly OnboardingService _svc;
    private readonly IInstallationStateService _installationState;

    public SetupController(
        WizardStateService state,
        OnboardingService svc,
        IInstallationStateService installationState)
    {
        _state = state;
        _svc = svc;
        _installationState = installationState;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var isInstalled = _installationState.IsInstalled();
        var step = DetermineNextStep();

        return Ok(new
        {
            isInstalled,
            currentStep = step,
            completedSteps = new[] { 1, 2, 3, 5, 6, 7, 8, 9, 10, 11 }
        });
    }

    private int DetermineNextStep()
    {
        if (_state.License == null) return 1;
        if (string.IsNullOrEmpty(_state.DbHost)) return 3;
        if (string.IsNullOrEmpty(_state.RedisHost)) return 4;
        if (string.IsNullOrEmpty(_state.UniversityDomain)) return 9;
        return 11;
    }

    [HttpPost("step1/license")]
    public async Task<IActionResult> ValidateLicense([FromBody] LicenseKeyRequest r, CancellationToken ct)
    {
        var result = await _svc.ValidateLicenseAsync(_state, r.LicenseKey, ct);
        return Ok(result);
    }

    [HttpPost("step2/deployment-type")]
    public IActionResult SetDeploymentType([FromBody] DeploymentTypeRequest r)
    {
        _state.FileStorageProvider = r.FileStorageProvider;
        _state.SecretsProvider = r.SecretsProvider;
        return Ok(new { success = true });
    }

    [HttpPost("step3/database")]
    [HttpPost("step3/test-postgres")]
    public async Task<IActionResult> ConfigureDatabase([FromBody] DatabaseConfigRequest r, CancellationToken ct)
    {
        _state.DbHost = r.Host;
        _state.DbPort = r.Port;
        _state.DbName = r.DatabaseName;
        _state.DbUsername = r.Username;
        _state.DbPassword = r.Password;
        _state.DbSslEnabled = r.SslEnabled;

        var result = await _svc.TestPostgresAsync(_state, r.Host, r.Port, r.DatabaseName, r.Username, r.Password, ct);
        return Ok(result);
    }

    [HttpPost("step4/redis")]
    [HttpPost("step4/test-redis")]
    public async Task<IActionResult> ConfigureRedis([FromBody] RedisConfigRequest r, CancellationToken ct)
    {
        _state.RedisHost = r.Host;
        _state.RedisPort = r.Port;
        _state.RedisPassword = r.Password;
        _state.RedisTlsEnabled = r.TlsEnabled;

        var result = await _svc.TestRedisAsync(_state, r.Host, r.Port, r.Password, ct);
        return Ok(result);
    }

    [HttpPost("step5/license-info")]
    public async Task<IActionResult> ConfigureLicense([FromBody] LicenseInfoRequest r, CancellationToken ct)
    {
        _state.LicenseKey = r.LicenseKey;
        _state.UniversityFullName = r.InstitutionName;
        _state.UniversityShortName = r.Organization;
        _state.AdminEmail = r.AdministratorEmail;

        var result = await _svc.ValidateLicenseAsync(_state, r.LicenseKey, ct);
        _state.License = result.License;
        return Ok(result);
    }

    [HttpPost("step6/administrator")]
    public IActionResult CreateAdministrator([FromBody] AdministratorRequest r, CancellationToken ct)
    {
        _state.AdminFullName = r.FullName;
        _state.AdminAccountEmail = r.Email;

        return Ok(new { success = true, message = "Administrator account will be created during installation" });
    }

    [HttpPost("step7/smtp")]
    [HttpPost("step7/test-smtp")]
    public async Task<IActionResult> ConfigureSmtp([FromBody] SmtpConfigRequest r, CancellationToken ct)
    {
        _state.SmtpHost = r.Host;
        _state.SmtpPort = r.Port;
        _state.SmtpUsername = r.Username;
        _state.SmtpPassword = r.Password;
        _state.SmtpFromAddress = r.FromAddress;
        _state.SmtpFromName = r.FromName;

        var result = await _svc.TestSmtpAsync(_state, r.Host, r.Port, r.Username, r.Password, r.FromAddress, r.FromName, ct);
        return Ok(result);
    }

    [HttpPost("step8/storage")]
    public IActionResult ConfigureStorage([FromBody] StorageConfigRequest r)
    {
        if (r.Provider == "MinIO")
        {
            _state.FileStorageProvider = FileStorageProvider.AzureBlob;
            _state.MinioEndpoint = r.MinioEndpoint;
            _state.MinioAccessKey = r.MinioAccessKey;
            _state.MinioSecretKey = r.MinioSecretKey;
            _state.MinioBucketName = r.MinioBucketName;
        }

        return Ok(new { success = true });
    }

    [HttpPost("step9/domain")]
    public IActionResult ConfigureDomain([FromBody] DomainConfigRequest r)
    {
        _state.UniversityDomain = r.Domain;
        return Ok(new { success = true, erpUrl = $"https://erp.{r.Domain}" });
    }

    [HttpPost("step10/install")]
    public async Task<IActionResult> Install([FromBody] InstallRequest r, CancellationToken ct)
    {
        var result = await _svc.GoLiveAsync(_state, ct);
        if (result.Success)
        {
            await _installationState.MarkAsInstalledAsync(ct);
        }
        return Ok(result);
    }
}

public record LicenseKeyRequest(string LicenseKey);
public record DeploymentTypeRequest(FileStorageProvider FileStorageProvider, SecretsProvider SecretsProvider);
public record DatabaseConfigRequest(string Host, int Port, string DatabaseName, string Username, string Password, bool SslEnabled);
public record RedisConfigRequest(string Host, int Port, string? Password, bool TlsEnabled, bool TestConnection);
public record LicenseInfoRequest(string LicenseKey, string InstitutionName, string Organization, string AdministratorEmail);
public record AdministratorRequest(string FullName, string Email, string Password, string ConfirmPassword);
public record SmtpConfigRequest(string Host, int Port, string Username, string Password, string FromAddress, string FromName, bool TestConnection);
public record StorageConfigRequest(string Provider, string? MinioEndpoint, string? MinioAccessKey, string? MinioSecretKey, string? MinioBucketName);
public record DomainConfigRequest(string Domain);
public record InstallRequest(bool SkipOptionalIntegrations = true);
public record GoLiveResult(bool Success, string? ErpUrl = null, List<GoLiveCheck>? Checks = null);
