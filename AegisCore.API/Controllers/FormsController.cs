using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Forms;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormService _formService;
    private readonly IFormDefinitionRepository _formRepo;
    private readonly IFormSubmissionRepository _submissionRepo;
    private readonly ILogger<FormsController> _logger;

    public FormsController(
        IFormService formService,
        IFormDefinitionRepository formRepo,
        IFormSubmissionRepository submissionRepo,
        ILogger<FormsController> logger)
    {
        _formService = formService;
        _formRepo = formRepo;
        _submissionRepo = submissionRepo;
        _logger = logger;
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormRequest request, CancellationToken cancellationToken)
    {
        var form = new FormDefinition
        {
            Id = Guid.NewGuid(), Title = request.Title, Description = request.Description,
            AllowMultipleSubmissions = request.AllowMultipleSubmissions,
            AllowDraftSave = request.AllowDraftSave, OpenAt = request.OpenAt, CloseAt = request.CloseAt,
            AccessRoles = request.AccessRoles, WorkflowDefinitionId = request.WorkflowDefinitionId,
            CreatedByUserId = Guid.Empty, Status = FormStatus.Draft
        };

        var created = await _formService.CreateFormAsync(form, request.Fields, cancellationToken);

        return Ok(new FormDto
        {
            Id = created.Id, Title = created.Title, Description = created.Description,
            Status = created.Status.ToString(), Fields = request.Fields,
            PageCount = request.Fields.GroupBy(f => f.Page).Count(),
            AllowMultipleSubmissions = created.AllowMultipleSubmissions,
            AllowDraftSave = created.AllowDraftSave, OpenAt = created.OpenAt, CloseAt = created.CloseAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListForms([FromQuery] string? category, CancellationToken cancellationToken)
    {
        IReadOnlyList<FormDefinition> forms;
        if (!string.IsNullOrWhiteSpace(category))
            forms = await _formRepo.GetByCategoryAsync(category, cancellationToken);
        else
            forms = await _formRepo.GetPublishedAsync(cancellationToken);

        var response = forms.Select(f =>
        {
            var fields = JsonSerializer.Deserialize<List<FormFieldConfig>>(f.FieldsJson) ?? new();
            return new FormDto
            {
                Id = f.Id, Title = f.Title, Description = f.Description, Status = f.Status.ToString(),
                Fields = fields, PageCount = fields.GroupBy(x => x.Page).Count(),
                AllowMultipleSubmissions = f.AllowMultipleSubmissions,
                AllowDraftSave = f.AllowDraftSave, OpenAt = f.OpenAt, CloseAt = f.CloseAt,
                SubmissionCount = f.Submissions?.Count ?? 0, CreatedAt = f.CreatedAt
            };
        });

        return Ok(new { total = response.Count(), forms = response });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetForm(Guid id, CancellationToken cancellationToken)
    {
        var form = await _formRepo.GetByIdAsync(id, cancellationToken);
        if (form == null) return NotFound();

        var fields = JsonSerializer.Deserialize<List<FormFieldConfig>>(form.FieldsJson) ?? new();

        return Ok(new FormDto
        {
            Id = form.Id, Title = form.Title, Description = form.Description, Status = form.Status.ToString(),
            Fields = fields, PageCount = fields.GroupBy(x => x.Page).Count(),
            AllowMultipleSubmissions = form.AllowMultipleSubmissions,
            AllowDraftSave = form.AllowDraftSave, OpenAt = form.OpenAt, CloseAt = form.CloseAt,
            SubmissionCount = form.Submissions?.Count ?? 0, CreatedAt = form.CreatedAt
        });
    }

    [HttpPost("{formId:guid}/submit")]
    public async Task<IActionResult> SubmitForm(Guid formId, [FromBody] SubmitFormRequest request, CancellationToken cancellationToken)
    {
        var submission = await _formService.SubmitFormAsync(formId, request.Values, null, null, null, cancellationToken);
        return Ok(new SubmissionDto { Id = submission.Id, Status = submission.Status.ToString(), SubmittedAt = submission.SubmittedAt });
    }

    [HttpPost("{formId:guid}/draft")]
    public async Task<IActionResult> SaveDraft(Guid formId, [FromBody] SubmitFormRequest request, CancellationToken cancellationToken)
    {
        var draft = await _formService.SaveDraftAsync(formId, request.Values, null, null, cancellationToken);
        return Ok(new SubmissionDto { Id = draft.Id, Status = draft.Status.ToString() });
    }

    [HttpGet("{formId:guid}/submissions")]
    public async Task<IActionResult> ListSubmissions(Guid formId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        IReadOnlyList<FormSubmission> submissions;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubmissionStatus>(status, true, out var s))
            submissions = await _submissionRepo.GetByStatusAsync(formId, s, cancellationToken);
        else
            submissions = await _submissionRepo.GetByFormAsync(formId, cancellationToken);

        var form = await _formRepo.GetByIdAsync(formId, cancellationToken);

        var response = submissions.Select(s =>
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(s.ValuesJson) ?? new();
            return new SubmissionDto
            {
                Id = s.Id, FormTitle = form?.Title ?? "", SubmittedByName = s.SubmittedByName,
                SubmittedByEmail = s.SubmittedByEmail, Status = s.Status.ToString(),
                Values = values, CreatedAt = s.CreatedAt, SubmittedAt = s.SubmittedAt
            };
        });

        return Ok(new { total = response.Count(), submissions = response });
    }

    [HttpPut("submissions/{submissionId:guid}/status")]
    public async Task<IActionResult> UpdateSubmissionStatus(Guid submissionId, [FromQuery] string status, [FromQuery] string? notes, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SubmissionStatus>(status, true, out var s))
            return BadRequest(new { error = "invalid_status" });

        var submission = await _formService.UpdateSubmissionStatusAsync(submissionId, s, notes, cancellationToken);
        return Ok(new SubmissionDto { Id = submission.Id, Status = submission.Status.ToString() });
    }

    [HttpGet("{formId:guid}/export")]
    public async Task<IActionResult> ExportSubmissions(Guid formId, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var data = await _formService.ExportSubmissionsAsync(formId, format, cancellationToken);
        var contentType = format.ToLowerInvariant() == "excel"
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

        return File(data, contentType, $"submissions_{formId}.{format}");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("templates/{templateName}")]
    public async Task<IActionResult> CreateFromTemplate(string templateName, CancellationToken cancellationToken)
    {
        var form = await _formService.CreateFromTemplateAsync(templateName, Guid.Empty, cancellationToken);
        return Ok(new FormDto { Id = form.Id, Title = form.Title, Status = form.Status.ToString() });
    }
}
