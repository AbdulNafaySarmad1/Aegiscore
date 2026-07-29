using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class FormDefinitionRepository : Repository<FormDefinition>, IFormDefinitionRepository
{
    public FormDefinitionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<FormDefinition>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.Status == FormStatus.Published).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormDefinition>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.Category == category).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormDefinition>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.IsTemplate).ToListAsync(cancellationToken);
    }
}

public class FormSubmissionRepository : Repository<FormSubmission>, IFormSubmissionRepository
{
    public FormSubmissionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(s => s.Form).Where(s => s.FormId == formId).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByStatusAsync(Guid formId, SubmissionStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.FormId == formId && s.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(s => s.Form).Where(s => s.SubmittedByUserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<FormSubmission?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.FormId == formId && s.SubmittedByUserId == userId, cancellationToken);
    }
}
