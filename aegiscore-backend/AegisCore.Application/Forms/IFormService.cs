using AegisCore.Domain.Entities;

namespace AegisCore.Application.Forms;

public interface IFormService
{
    Task<FormDefinition> CreateFormAsync(FormDefinition form, List<FormFieldConfig> fields, CancellationToken cancellationToken = default);
    Task<FormSubmission> SubmitFormAsync(Guid formId, Dictionary<string, object?> values, Guid? userId, string? userName, string? userEmail, CancellationToken cancellationToken = default);
    Task<FormSubmission> SaveDraftAsync(Guid formId, Dictionary<string, object?> values, Guid? userId, string? userEmail, CancellationToken cancellationToken = default);
    Task<FormSubmission> UpdateSubmissionStatusAsync(Guid submissionId, SubmissionStatus status, string? notes, CancellationToken cancellationToken = default);
    Task<byte[]> ExportSubmissionsAsync(Guid formId, string format, CancellationToken cancellationToken = default);
    Task<FormDefinition> CreateFromTemplateAsync(string templateName, Guid createdByUserId, CancellationToken cancellationToken = default);
}
