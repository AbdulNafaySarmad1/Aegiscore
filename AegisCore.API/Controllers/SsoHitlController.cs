using AegisCore.Application.SsoHitl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/u14")]
[Authorize]
public sealed class SsoHitlController(IMultiTenantSsoService sso, ITwoFactorService twoFactor, IAgenticExamService exams) : ControllerBase
{
    [HttpPost("auth/detect-provider")]
    [AllowAnonymous]
    public async Task<IActionResult> Detect([FromBody] DetectProviderRequest request, CancellationToken ct)
        => Ok(await sso.DetectProviderAsync(request.Email, request.ReturnUrl, ct));

    [HttpPost("auth/temp-password")]
    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    public IActionResult TempPassword() => Ok(new { password = sso.GenerateTemporaryPassword() });

    [HttpPost("auth/2fa/setup")]
    public IActionResult Setup2Fa()
    {
        var secret = twoFactor.GenerateSecret();
        return Ok(new { secret, backupCodes = twoFactor.GenerateBackupCodes(), otpauth = $"otpauth://totp/AegisCore:user?secret={secret}&issuer=AegisCore" });
    }

    [HttpPost("auth/2fa/verify")]
    public IActionResult Verify2Fa([FromBody] VerifyTotpRequest request)
        => Ok(new { valid = twoFactor.VerifyTotp(request.Secret, request.Code, DateTimeOffset.UtcNow) });

    [HttpGet("exams/qr")]
    [AegisCore.API.Authorization.RequirePermission("System", "Read")]
    public IActionResult Qr([FromQuery] Guid examId, [FromQuery] Guid studentId)
        => Ok(new { payload = exams.BuildQrPayload(examId, studentId) });

    [HttpGet("exams/review-required")]
    [AegisCore.API.Authorization.RequirePermission("System", "Approve")]
    public IActionResult ReviewRequired([FromQuery] decimal confidence)
        => Ok(new { required = exams.RequiresHumanReview(confidence) });
}

public sealed record DetectProviderRequest(string Email, string ReturnUrl);
public sealed record VerifyTotpRequest(string Secret, string Code);
