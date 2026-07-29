using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
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

    [HttpGet("permissions")]
    public IActionResult Permissions() => BeforeInstall(() => Ok(_service.GetPermissions()));

    [HttpPost("permissions")]
    public IActionResult SavePermissions([FromBody] SavePermissionsRequest request) => BeforeInstall(() =>
    {
        try { return Ok(_service.SavePermissions(request)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    });

    [HttpGet("review")]
    public IActionResult Review() => BeforeInstall(() => Ok(_service.Review(_wizard)));

    private IActionResult BeforeInstall(Func<IActionResult> action) =>
        _installation.IsInstalled() ? NotFound() : action();
}

