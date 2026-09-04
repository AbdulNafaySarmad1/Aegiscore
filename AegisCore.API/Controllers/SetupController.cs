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
    private readonly SetupImportService _imports;

    public SetupController(
        WizardStateService state,
        OnboardingService service,
        EnhancedSetupService enhanced,
        IInstallationStateService installationState,
        SetupImportService imports)
    {
        _state = state;
        _service = service;
        _enhanced = enhanced;
        _installationState = installationState;
        _imports = imports;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        return Ok(new
        {
            isInstalled = _installationState.IsInstalled(),
            currentStep = DetermineNextStep(state),
            review = _enhanced.Review(state)
        });
    }

    private int DetermineNextStep(WizardStateDto state)
    {
        if (state.License is null) return 1;
        if (string.IsNullOrEmpty(state.DbHost) || string.IsNullOrEmpty(state.RedisHost)) return 2;
        var review = _enhanced.Review(state);
        if (review.Hierarchy is null) return 4;
        if (!review.Checks.First(x => x.Name == "Required master data valid").Passed) return 5;
        if (!review.Checks.First(x => x.Name == "RBAC configured").Passed) return 6;
        return 7;
    }

    [HttpPost("step1/license")]
    [HttpPost("step1/validate-license")]
    public async Task<IActionResult> ValidateLicense([FromBody] LicenseKeyRequest request, CancellationToken ct)
    {
        await LoadAsync(ct);
        var state = await _state.InitializeTenantAtStepOneAsync(SetupToken(), ct);
        var result = await _service.ValidateLicenseAsync(state, request.LicenseKey, ct);
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(result);
    }

    [HttpPost("step2/deployment-type")]
    public async Task<IActionResult> SetDeploymentType([FromBody] DeploymentTypeRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        state.FileStorageProvider = request.FileStorageProvider;
        state.SecretsProvider = request.SecretsProvider;
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(new { success = true });
    }

    [HttpPost("step3/database")]
    [HttpPost("step3/test-postgres")]
    public async Task<IActionResult> ConfigureDatabase([FromBody] DatabaseConfigRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        state.DbSslEnabled = request.SslEnabled;
        var result = await _service.TestPostgresAsync(state, request.Host, request.Port, request.DatabaseName, request.Username, request.Password, ct);
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(result);
    }

    [HttpPost("step4/redis")]
    [HttpPost("step4/test-redis")]
    public async Task<IActionResult> ConfigureRedis([FromBody] RedisConfigRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        state.RedisTlsEnabled = request.TlsEnabled;
        var result = await _service.TestRedisAsync(state, request.Host, request.Port, request.Password, ct);
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(result);
    }

    [HttpPost("step5/license-info")]
    public async Task<IActionResult> ConfigureLicense([FromBody] LicenseInfoRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        state.UniversityFullName = request.InstitutionName.Trim();
        state.UniversityShortName = request.Organization.Trim();
        state.AdminEmail = request.AdministratorEmail.Trim();
        var result = await _service.ValidateLicenseAsync(state, request.LicenseKey, ct);
        state.License = result.License;
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(result);
    }

    [HttpPost("step6/administrator")]
    public async Task<IActionResult> CreateAdministrator([FromBody] AdministratorRequest request, CancellationToken ct)
    {
        if (request.Password.Length < 12)
            return BadRequest(new { error = "Administrator password must be at least 12 characters." });
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(new { error = "Administrator passwords do not match." });
        var state = await LoadAsync(ct);
        state.AdminFullName = request.FullName.Trim();
        state.AdminAccountEmail = request.Email.Trim();
        state.AdminPassword = request.Password;
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(new { success = true, message = "Administrator account is queued for creation during installation." });
    }

    [HttpPost("step7/smtp")]
    [HttpPost("step7/test-smtp")]
    public async Task<IActionResult> ConfigureSmtp([FromBody] SmtpConfigRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        var result = await _service.TestSmtpAsync(state, request.Host, request.Port, request.Username, request.Password, request.FromAddress, request.FromName, ct);
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(result);
    }

    [HttpPost("step8/storage")]
    public async Task<IActionResult> ConfigureStorage([FromBody] StorageConfigRequest request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        if (request.Provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
        {
            state.FileStorageProvider = FileStorageProvider.MinIO;
            state.MinioEndpoint = request.MinioEndpoint;
            state.MinioAccessKey = request.MinioAccessKey;
            state.MinioSecretKey = request.MinioSecretKey;
            state.MinioBucketName = request.MinioBucketName;
        }
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(new { success = true });
    }

    [HttpPost("step9/domain")]
    [HttpPost("step9/provision-domain")]
    public async Task<IActionResult> ConfigureDomain([FromBody] DomainConfigRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { error = "University domain is required." });
        var state = await LoadAsync(ct);
        var result = _service.ProvisionDomain(state, request.Domain.Trim());
        await _state.SaveState(SetupToken(), state, ct);
        return Ok(new { result.ErpUrl, instructions = _service.GenerateDnsInstructions(state) });
    }

    [HttpGet("step9/check-dns")]
    public async Task<IActionResult> CheckDns(CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        return Ok(await _service.CheckDnsPropagationAsync(state, ct));
    }

    [HttpPost("step10/install")]
    [HttpPost("step11/go-live")]
    public async Task<IActionResult> Install([FromBody] InstallRequest? request, CancellationToken ct)
    {
        var state = await LoadAsync(ct);
        var enhancedReview = _enhanced.Review(state);
        if (!enhancedReview.Ready)
            return UnprocessableEntity(new { success = false, error = "Enhanced setup checks are incomplete.", review = enhancedReview });

        var import = await _imports.CommitAsync(state.TenantId, state.AdminFullName, state.AdminAccountEmail, state.AdminPassword, _enhanced.GetValidatedRows(), ct);
        if (!import.Success)
            return UnprocessableEntity(new { success = false, error = new WizardError("Initial data import failed.", string.Join(" ", import.RejectedRows)) });

        var result = await _service.GoLiveAsync(state, ct);
        if (result.Success)
        {
            state.CompletedAt = DateTimeOffset.UtcNow;
            await _state.SaveState(SetupToken(), state, ct);
        }
        if (result.Success) await _installationState.MarkAsInstalledAsync(ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    private Task<WizardStateDto> LoadAsync(CancellationToken ct)
        => _state.GetOrCreateStateAsync(SetupToken(), HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    private string SetupToken()
        => Request.Headers["X-Setup-Token"].FirstOrDefault()
            ?? HttpContext.RequestServices.GetRequiredService<IConfiguration>()["AEGISCORE_SETUP_TOKEN"]
            ?? throw new InvalidOperationException("AEGISCORE_SETUP_TOKEN is required.");
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
