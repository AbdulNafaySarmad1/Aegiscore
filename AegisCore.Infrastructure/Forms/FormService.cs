using System.Text;
using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Forms;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Forms;

public class FormService : IFormService
{
    private readonly IFormDefinitionRepository _formRepo;
    private readonly IFormSubmissionRepository _submissionRepo;
    private readonly ILogger<FormService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FormService(
        IFormDefinitionRepository formRepo,
        IFormSubmissionRepository submissionRepo,
        ILogger<FormService> logger)
    {
        _formRepo = formRepo;
        _submissionRepo = submissionRepo;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<FormDefinition> CreateFormAsync(FormDefinition form, List<FormFieldConfig> fields, CancellationToken cancellationToken = default)
    {
        form.FieldsJson = JsonSerializer.Serialize(fields, _jsonOptions);
        form.PagesJson = JsonSerializer.Serialize(fields.GroupBy(f => f.Page).Select(g => new { page = g.Key, count = g.Count() }), _jsonOptions);
        await _formRepo.AddAsync(form, cancellationToken);

        _logger.LogInformation("Form created: {Title} with {FieldCount} fields", form.Title, fields.Count);
        return form;
    }

    public async Task<FormSubmission> SubmitFormAsync(Guid formId, Dictionary<string, object?> values, Guid? userId, string? userName, string? userEmail, CancellationToken cancellationToken = default)
    {
        var form = await _formRepo.GetByIdAsync(formId, cancellationToken)
            ?? throw new KeyNotFoundException("Form not found.");

        if (form.Status != FormStatus.Published)
            throw new InvalidOperationException("Form is not accepting submissions.");

        if (form.CloseAt.HasValue && DateTime.UtcNow > form.CloseAt.Value)
            throw new InvalidOperationException("Form submission window has closed.");

        if (!form.AllowMultipleSubmissions && userId.HasValue)
        {
            var existing = await _submissionRepo.GetByFormAndUserAsync(formId, userId.Value, cancellationToken);
            if (existing != null && existing.Status != SubmissionStatus.Draft)
                throw new InvalidOperationException("Only one submission is allowed per user.");
        }

        var submission = new FormSubmission
        {
            Id = Guid.NewGuid(), FormId = formId,
            SubmittedByUserId = userId, SubmittedByName = userName, SubmittedByEmail = userEmail,
            Status = SubmissionStatus.Submitted, ValuesJson = JsonSerializer.Serialize(values, _jsonOptions),
            SubmittedAt = DateTime.UtcNow, TenantId = form.TenantId
        };

        await _submissionRepo.AddAsync(submission, cancellationToken);
        return submission;
    }

    public async Task<FormSubmission> SaveDraftAsync(Guid formId, Dictionary<string, object?> values, Guid? userId, string? userEmail, CancellationToken cancellationToken = default)
    {
        var form = await _formRepo.GetByIdAsync(formId, cancellationToken)
            ?? throw new KeyNotFoundException("Form not found.");

        if (!form.AllowDraftSave)
            throw new InvalidOperationException("Draft saving is not enabled for this form.");

        FormSubmission? submission = null;
        if (userId.HasValue)
            submission = await _submissionRepo.GetByFormAndUserAsync(formId, userId.Value, cancellationToken);

        if (submission != null && submission.Status == SubmissionStatus.Draft)
        {
            submission.ValuesJson = JsonSerializer.Serialize(values, _jsonOptions);
            await _submissionRepo.UpdateAsync(submission, cancellationToken);
            return submission;
        }

        submission = new FormSubmission
        {
            Id = Guid.NewGuid(), FormId = formId,
            SubmittedByUserId = userId, SubmittedByEmail = userEmail,
            Status = SubmissionStatus.Draft, ValuesJson = JsonSerializer.Serialize(values, _jsonOptions),
            TenantId = form.TenantId
        };

        await _submissionRepo.AddAsync(submission, cancellationToken);
        return submission;
    }

    public async Task<FormSubmission> UpdateSubmissionStatusAsync(Guid submissionId, SubmissionStatus status, string? notes, CancellationToken cancellationToken = default)
    {
        var submission = await _submissionRepo.GetByIdAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("Submission not found.");

        submission.Status = status;
        if (status is SubmissionStatus.Approved or SubmissionStatus.Rejected)
            submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewNotes = notes;

        await _submissionRepo.UpdateAsync(submission, cancellationToken);
        return submission;
    }

    public async Task<byte[]> ExportSubmissionsAsync(Guid formId, string format, CancellationToken cancellationToken = default)
    {
        var submissions = await _submissionRepo.GetByFormAsync(formId, cancellationToken);
        var form = await _formRepo.GetByIdAsync(formId, cancellationToken);

        return format.ToLowerInvariant() switch
        {
            "csv" => ExportToCsv(submissions, form),
            "excel" => ExportToCsv(submissions, form),
            _ => ExportToCsv(submissions, form)
        };
    }

    public async Task<FormDefinition> CreateFromTemplateAsync(string templateName, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var fields = GetTemplateFields(templateName);
        var form = new FormDefinition
        {
            Id = Guid.NewGuid(), Title = templateName, IsTemplate = false,
            AllowMultipleSubmissions = true, AllowDraftSave = true,
            CreatedByUserId = createdByUserId, Status = FormStatus.Draft
        };

        return await CreateFormAsync(form, fields, cancellationToken);
    }

    private static byte[] ExportToCsv(IReadOnlyList<FormSubmission> submissions, FormDefinition? form)
    {
        var sb = new StringBuilder();
        var fields = JsonSerializer.Deserialize<List<FormFieldConfig>>(form?.FieldsJson ?? "[]");

        if (fields != null && fields.Count > 0)
        {
            sb.AppendLine(string.Join(",", fields.Select(f => $"\"{f.Label}\"")));
            sb.Append("Status"); sb.AppendLine(",SubmittedAt");
        }

        foreach (var submission in submissions)
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(submission.ValuesJson) ?? new();

            if (fields != null)
            {
                foreach (var field in fields)
                {
                    var val = values.TryGetValue(field.Id, out var v) ? v.GetString() ?? "" : "";
                    sb.Append($"\"{val}\","); 
                }
            }

            sb.Append($"\"{submission.Status}\",\"{submission.SubmittedAt}\"");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static List<FormFieldConfig> GetTemplateFields(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "admission form" => new List<FormFieldConfig>
            {
                new() { Id = "f1", Type = "Text", Label = "Full Name", Required = true, SortOrder = 1 },
                new() { Id = "f2", Type = "Email", Label = "Email Address", Required = true, SortOrder = 2 },
                new() { Id = "f3", Type = "Phone", Label = "Phone Number", Required = true, SortOrder = 3 },
                new() { Id = "f4", Type = "Date", Label = "Date of Birth", Required = true, SortOrder = 4 },
                new() { Id = "f5", Type = "Dropdown", Label = "Program", Required = true, Options = new() { new() { Label = "BS CS", Value = "bs_cs" }, new() { Label = "BS SE", Value = "bs_se" } }, SortOrder = 5 },
                new() { Id = "f6", Type = "TextArea", Label = "Statement of Purpose", Required = true, SortOrder = 6 },
                new() { Id = "f7", Type = "FileUpload", Label = "Upload Transcript", SortOrder = 7 }
            },
            "leave request" => new List<FormFieldConfig>
            {
                new() { Id = "f1", Type = "Dropdown", Label = "Leave Type", Required = true, Options = new() { new() { Label = "Annual", Value = "annual" }, new() { Label = "Medical", Value = "medical" } }, SortOrder = 1 },
                new() { Id = "f2", Type = "Date", Label = "Start Date", Required = true, SortOrder = 2 },
                new() { Id = "f3", Type = "Date", Label = "End Date", Required = true, SortOrder = 3 },
                new() { Id = "f4", Type = "TextArea", Label = "Reason", Required = true, SortOrder = 4 }
            },
            "feedback survey" => new List<FormFieldConfig>
            {
                new() { Id = "f1", Type = "Radio", Label = "Overall Experience", Required = true, Options = new() { new() { Label = "Excellent", Value = "5" }, new() { Label = "Good", Value = "4" }, new() { Label = "Average", Value = "3" } }, SortOrder = 1 },
                new() { Id = "f2", Type = "TextArea", Label = "Comments", SortOrder = 2 }
            },
            _ => new List<FormFieldConfig>
            {
                new() { Id = "f1", Type = "Text", Label = "Name", Required = true, SortOrder = 1 },
                new() { Id = "f2", Type = "TextArea", Label = "Details", SortOrder = 2 }
            }
        };
    }
}
