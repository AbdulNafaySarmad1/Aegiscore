using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/setup/enhanced")]
[RequestSizeLimit(25 * 1024 * 1024)]
public sealed class EnhancedSetupController : ControllerBase
{
    private readonly EnhancedSetupService _service;
    private readonly WizardStateService _wizard;
    private readonly IInstallationStateService _installation;

    public EnhancedSetupController(
        EnhancedSetupService service,
        WizardStateService wizard,
        IInstallationStateService installation)
    {
        _service = service;
        _wizard = wizard;
        _installation = installation;
    }

    [HttpGet("templates")]
    public IActionResult Templates() => BeforeInstall(() => Ok(_service.GetTemplates()));

    [HttpGet("templates/all")]
    public IActionResult AllTemplates() => BeforeInstall(() =>
        File(_service.GenerateAllTemplates(), "application/zip", "aegiscore-import-templates.zip"));

    [HttpGet("templates/{fileType}")]
    public IActionResult Template(string fileType) => BeforeInstall(() =>
        File(_service.GenerateTemplate(fileType),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{fileType}-template.xlsx"));

    [HttpGet("hierarchy")]
    public IActionResult Hierarchy() => BeforeInstall(() => Ok(_service.GetHierarchy()));

    [HttpPost("hierarchy")]
    public IActionResult SaveHierarchy([FromBody] SaveHierarchyRequest request) => BeforeInstall(() =>
    {
        try { return Ok(_service.SaveHierarchy(request)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    });

    [HttpPost("uploads/{fileType}/validate")]
    public async Task<IActionResult> Validate(string fileType, IFormFile file, CancellationToken ct)
    {
        if (_installation.IsInstalled()) return NotFound();
        var result = await _service.ValidateAsync(fileType, file, ct);
        return result.IsValid ? Ok(result) : UnprocessableEntity(result);
    }

    [HttpGet("uploads/{fileType}/preview")]
    public IActionResult Preview(string fileType) => BeforeInstall(() =>
    {
        var rows = _service.GetValidatedRows();
        if (!rows.TryGetValue(fileType, out var source) || source.Count == 0) return NotFound();
        return Ok(new { fileType, headers = source[0], rows = source.Skip(1).Take(25), totalRows = source.Count - 1 });
    });

    [HttpGet("uploads/{fileType}/rejections")]
    public async Task<IActionResult> Rejections(string fileType, CancellationToken ct)
    {
        if (_installation.IsInstalled()) return NotFound();
        var review = _service.Review(await _wizard.ResumeWizardAsync(SetupToken(), ct));
        return review.Uploads.TryGetValue(fileType, out var upload)
            ? Ok(new { fileType, errors = upload.Errors, warnings = upload.Warnings })
            : NotFound();
    }

    [HttpGet("permissions")]
    public IActionResult Permissions() => BeforeInstall(() => Ok(_service.GetPermissions()));

    [HttpPost("permissions")]
    public IActionResult SavePermissions([FromBody] SavePermissionsRequest request) => BeforeInstall(() =>
    {
        try { return Ok(_service.SavePermissions(request)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    });

    [HttpGet("review")]
    public async Task<IActionResult> Review(CancellationToken ct)
    {
        if (_installation.IsInstalled()) return NotFound();
        return Ok(_service.Review(await _wizard.ResumeWizardAsync(SetupToken(), ct)));
    }

    private string SetupToken()
        => Request.Headers["X-Setup-Token"].FirstOrDefault()
            ?? HttpContext.RequestServices.GetRequiredService<IConfiguration>()["AEGISCORE_SETUP_TOKEN"]
            ?? throw new InvalidOperationException("AEGISCORE_SETUP_TOKEN is required.");

    private IActionResult BeforeInstall(Func<IActionResult> action) =>
        _installation.IsInstalled() ? NotFound() : action();
}
