using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly WizardStateService _state;
    private readonly OnboardingService _service;
    private readonly EnhancedSetupService _enhanced;
    private readonly IInstallationStateService _installationState;

    public SetupController(
        WizardStateService state,
        OnboardingService service,
        EnhancedSetupService enhanced,
        IInstallationStateService installationState)
    {
        _state = state;
        _service = service;
        _enhanced = enhanced;
        _installationState = installationState;
    }

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new
    {
        isInstalled = _installationState.IsInstalled(),
        currentStep = DetermineNextStep(),
        review = _enhanced.Review(_state)
    });

    private int DetermineNextStep()
    {
        if (_state.License is null) return 1;
        if (string.IsNullOrEmpty(_state.DbHost) || string.IsNullOrEmpty(_state.RedisHost)) return 2;
        var review = _enhanced.Review(_state);
        if (review.Hierarchy is null) return 4;
        if (!review.Checks.First(x => x.Name == "Required master data valid").Passed) return 5;
        if (!review.Checks.First(x => x.Name == "RBAC configured").Passed) return 6;
        return 7;
    }

    [HttpPost("step1/license")]
    [HttpPost("step1/validate-license")]
    public async Task<IActionResult> ValidateLicense([FromBody] LicenseKeyRequest request, CancellationToken ct) =>
        Ok(await _service.ValidateLicenseAsync(_state, request.LicenseKey, ct));

    [HttpPost("step2/deployment-type")]
    public IActionResult SetDeploymentType([FromBody] DeploymentTypeRequest request)
    {
        _state.FileStorageProvider = request.FileStorageProvider;
        _state.SecretsProvider = request.SecretsProvider;
        return Ok(new { success = true });
    }

    [HttpPost("step3/database")]
    [HttpPost("step3/test-postgres")]
    public async Task<IActionResult> ConfigureDatabase([FromBody] DatabaseConfigRequest request, CancellationToken ct)
    {
        _state.DbSslEnabled = request.SslEnabled;
        return Ok(await _service.TestPostgresAsync(_state, request.Host, request.Port, request.DatabaseName, request.Username, request.Password, ct));
    }

    [HttpPost("step4/redis")]
    [HttpPost("step4/test-redis")]
    public async Task<IActionResult> ConfigureRedis([FromBody] RedisConfigRequest request, CancellationToken ct)
    {
        _state.RedisTlsEnabled = request.TlsEnabled;
        return Ok(await _service.TestRedisAsync(_state, request.Host, request.Port, request.Password, ct));
    }

    [HttpPost("step5/license-info")]
    public async Task<IActionResult> ConfigureLicense([FromBody] LicenseInfoRequest request, CancellationToken ct)
    {
        _state.UniversityFullName = request.InstitutionName.Trim();
        _state.UniversityShortName = request.Organization.Trim();
        _state.AdminEmail = request.AdministratorEmail.Trim();
        var result = await _service.ValidateLicenseAsync(_state, request.LicenseKey, ct);
        _state.License = result.License;
        return Ok(result);
    }

    [HttpPost("step6/administrator")]
    public IActionResult CreateAdministrator([FromBody] AdministratorRequest request)
    {
        if (request.Password.Length < 12)
            return BadRequest(new { error = "Administrator password must be at least 12 characters." });
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(new { error = "Administrator passwords do not match." });
        _state.AdminFullName = request.FullName.Trim();
        _state.AdminAccountEmail = request.Email.Trim();
        return Ok(new { success = true, message = "Administrator account is queued for creation during installation." });
    }

    [HttpPost("step7/smtp")]
    [HttpPost("step7/test-smtp")]
    public async Task<IActionResult> ConfigureSmtp([FromBody] SmtpConfigRequest request, CancellationToken ct) =>
        Ok(await _service.TestSmtpAsync(_state, request.Host, request.Port, request.Username, request.Password, request.FromAddress, request.FromName, ct));

    [HttpPost("step8/storage")]
    public IActionResult ConfigureStorage([FromBody] StorageConfigRequest request)
    {
        if (request.Provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
        {
            _state.FileStorageProvider = FileStorageProvider.MinIO;
            _state.MinioEndpoint = request.MinioEndpoint;
            _state.MinioAccessKey = request.MinioAccessKey;
            _state.MinioSecretKey = request.MinioSecretKey;
            _state.MinioBucketName = request.MinioBucketName;
        }
        return Ok(new { success = true });
    }

    [HttpPost("step9/domain")]
    [HttpPost("step9/provision-domain")]
    public IActionResult ConfigureDomain([FromBody] DomainConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { error = "University domain is required." });
        var result = _service.ProvisionDomain(_state, request.Domain.Trim());
        return Ok(new { result.ErpUrl, instructions = _service.GenerateDnsInstructions(_state) });
    }

    [HttpGet("step9/check-dns")]
    public async Task<IActionResult> CheckDns(CancellationToken ct) =>
        Ok(await _service.CheckDnsPropagationAsync(_state, ct));

    [HttpPost("step10/install")]
    [HttpPost("step11/go-live")]
    public async Task<IActionResult> Install([FromBody] InstallRequest? request, CancellationToken ct)
    {
        var enhancedReview = _enhanced.Review(_state);
        if (!enhancedReview.Ready)
            return UnprocessableEntity(new { success = false, error = "Enhanced setup checks are incomplete.", review = enhancedReview });

        var result = await _service.GoLiveAsync(_state, ct);
        if (result.Success) await _installationState.MarkAsInstalledAsync(ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }
}

public record LicenseKeyRequest(string LicenseKey);
public record DeploymentTypeRequest(FileStorageProvider FileStorageProvider, SecretsProvider SecretsProvider);
public record DatabaseConfigRequest(string Host, int Port, string DatabaseName, string Username, string Password, bool SslEnabled = true);
public record RedisConfigRequest(string Host, int Port, string? Password, bool TlsEnabled = false, bool TestConnection = true);
public record LicenseInfoRequest(string LicenseKey, string InstitutionName, string Organization, string AdministratorEmail);
public record AdministratorRequest(string FullName, string Email, string Password, string ConfirmPassword);
public record SmtpConfigRequest(string Host, int Port, string Username, string Password, string FromAddress, string FromName, bool TestConnection = true);
public record StorageConfigRequest(string Provider, string? MinioEndpoint, string? MinioAccessKey, string? MinioSecretKey, string? MinioBucketName);
public record DomainConfigRequest(string Domain);
public record InstallRequest(bool SkipOptionalIntegrations = true);
