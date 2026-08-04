using AegisCore.Domain.Entities;

namespace AegisCore.Application.Campus;

public interface ICampusService
{
    Task<CampusDto> RegisterCampusAsync(CreateCampusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusDto>> GetCampusesAsync(CancellationToken cancellationToken = default);
    Task<CampusDto> UpdateCampusAsync(Guid campusId, UpdateCampusRequest request, CancellationToken cancellationToken = default);
    Task InitiateStudentTransferAsync(Guid studentId, Guid sourceCampusId, Guid destCampusId, string reason, CancellationToken cancellationToken = default);
    Task ApproveTransferAsync(Guid transferId, string approver, bool isSource, CancellationToken cancellationToken = default);
    Task<CampusConfiguration> GetCampusConfigAsync(Guid campusId, CancellationToken cancellationToken = default);
}

public interface IConsolidatedReportingService
{
    Task<UniversityReportDto> GetUniversityReportAsync(CancellationToken cancellationToken = default);
    Task<CampusComparisonDto> GetCampusComparisonAsync(CancellationToken cancellationToken = default);
}
