using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Forms;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Application.Integrations;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Integrations.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
    private readonly AegisDbContext _db;
    private readonly IFormIngestionService _ingestion;

    public FormsController(
        IFormService formService,
        IFormDefinitionRepository formRepo,
        IFormSubmissionRepository submissionRepo,
        ILogger<FormsController> logger,
        AegisDbContext db,
        IFormIngestionService ingestion)
    {
        _formService = formService;
        _formRepo = formRepo;
        _submissionRepo = submissionRepo;
        _logger = logger;
        _db = db;
        _ingestion = ingestion;
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
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
        await RecordOutcomeAnswersAsync(formId, submission.Id, request.Values, cancellationToken);
        return Ok(new SubmissionDto { Id = submission.Id, Status = submission.Status.ToString(), SubmittedAt = submission.SubmittedAt });
    }

    /// <summary>
    /// A native form wired to the outcome survey mapping (a form-integration source with the
    /// <c>aegiscore-forms</c> provider) feeds the same store as an external tool's webhook, so the
    /// indirect-assessment report counts both alike.
    /// </summary>
    private async Task RecordOutcomeAnswersAsync(Guid formId, Guid submissionId, Dictionary<string, object?> values, CancellationToken ct)
    {
        var formKey = formId.ToString();
        var source = await _db.FormIntegrationSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Provider == FormIngestionService.NativeProvider && s.ExternalFormId == formKey && s.IsActive, ct);
        if (source is null) return;
        var answers = new List<FormAnswer>();
        foreach (var (field, value) in values)
        {
            var element = JsonSerializer.SerializeToElement(value);
            FormPayloadText.Flatten(field, null, element, answers);
        }
        await _ingestion.IngestAsync(source.Id, new FormSubmissionEnvelope(formKey, submissionId.ToString(), answers), ct);
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
        var form = await _formRepo.GetByIdAsync(formId, cancellationToken);
        if (form is null) return NotFound(new { error = "Form was not found." });

        IReadOnlyList<FormSubmission> submissions;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubmissionStatus>(status, true, out var s))
            submissions = await _submissionRepo.GetByStatusAsync(formId, s, cancellationToken);
        else
            submissions = await _submissionRepo.GetByFormAsync(formId, cancellationToken);

        var response = submissions.Select(s =>
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(s.ValuesJson) ?? new();
            return new SubmissionDto
            {
                Id = s.Id, FormTitle = form.Title, SubmittedByName = s.SubmittedByName,
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
        // An export of a form that does not exist is an empty file that looks like a real one.
        if (await _formRepo.GetByIdAsync(formId, cancellationToken) is null) return NotFound(new { error = "Form was not found." });
        var data = await _formService.ExportSubmissionsAsync(formId, format, cancellationToken);
        var contentType = format.ToLowerInvariant() == "excel"
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

        return File(data, contentType, $"submissions_{formId}.{format}");
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPost("templates/{templateName}")]
    public async Task<IActionResult> CreateFromTemplate(string templateName, CancellationToken cancellationToken)
    {
        var form = await _formService.CreateFromTemplateAsync(templateName, Guid.Empty, cancellationToken);
        return Ok(new FormDto { Id = form.Id, Title = form.Title, Status = form.Status.ToString() });
    }
}
