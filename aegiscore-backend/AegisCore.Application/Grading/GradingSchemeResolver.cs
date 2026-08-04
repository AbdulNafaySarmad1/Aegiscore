using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;

namespace AegisCore.Application.Grading;

public class GradingSchemeResolver
{
    private readonly IUnitOfWork _unitOfWork;
    public GradingSchemeResolver(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<GradingScheme?> ResolveForCourseAsync(Guid courseId, Guid tenantId)
    {
        return await _unitOfWork.Courses.ExistsAsync(c => c.Id == courseId)
            ? GradingScheme.GPA4
            : null;
    }
}
