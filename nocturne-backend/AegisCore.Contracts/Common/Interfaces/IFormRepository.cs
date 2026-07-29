using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IFormDefinitionRepository : IRepository<FormDefinition>
{
    Task<IReadOnlyList<FormDefinition>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormDefinition>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormDefinition>> GetTemplatesAsync(CancellationToken cancellationToken = default);
}

public interface IFormSubmissionRepository : IRepository<FormSubmission>
{
    Task<IReadOnlyList<FormSubmission>> GetByFormAsync(Guid formId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByStatusAsync(Guid formId, SubmissionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FormSubmission?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default);
}
