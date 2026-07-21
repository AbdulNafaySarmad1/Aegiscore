using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;

namespace AegisCore.Application.Grading;

public class GradingSchemeResolver
{
    private readonly IUnitOfWork _unitOfWork;
    public GradingSchemeResolver(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<GradingScheme?> ResolveForCourseAsync(Guid courseId, Guid tenantId)
    {
        // IUnitOfWork does not have a Repository<T>() method.
        // Use the existing Courses repository to find the course's grading scheme.
        // For now, return the default grading scheme for the tenant.
        var courses = await _unitOfWork.Courses.GetAllAsync();
        var course = courses.FirstOrDefault(c => c.Id == courseId);
        if (course == null) return GradingScheme.GPA4;

        // Default: return GPA4 as the standard grading scheme
        return GradingScheme.GPA4;
    }
}
