using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;

namespace AegisCore.Application.Grading;

public class ResultService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GradingSchemeResolver _schemeResolver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResultService> _logger;

    public ResultService(
        IUnitOfWork unitOfWork,
        GradingSchemeResolver schemeResolver,
        IConfiguration configuration,
        ILogger<ResultService> logger)
    {
        _unitOfWork = unitOfWork;
        _schemeResolver = schemeResolver;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(string? grade, decimal? gpaPoints)> CalculateGradeAsync(
        Guid courseId, Guid tenantId, decimal marksObtained, decimal maxMarks)
    {
        var scheme = await _schemeResolver.ResolveForCourseAsync(courseId, tenantId);
        if (scheme == null)
        {
            return (null, null);
        }

        var percentage = maxMarks > 0 ? marksObtained / maxMarks * 100 : 0;

        // GradingScheme is an enum (Percentage, GPA4, GPA5, Letter), not an entity with Bands.
        // Return grade based on standard GPA4 thresholds.
        var (grade, gpaPoints) = scheme switch
        {
            GradingScheme.Percentage => (percentage >= 60 ? "Pass" : "Fail",
                                          percentage >= 60 ? (decimal?)null : null),
            _ => percentage switch
            {
                >= 90 => ("A+", 4.0m),
                >= 80 => ("A", 4.0m),
                >= 70 => ("B", 3.0m),
                >= 60 => ("C", 2.0m),
                >= 50 => ("D", 1.0m),
                _ => ("F", 0.0m)
            }
        };

        return (grade, gpaPoints);
    }
}
