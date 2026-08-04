using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface ISecurityIncidentRepository : IRepository<SecurityIncident>
{
    Task<IReadOnlyList<SecurityIncident>> GetByStatusAsync(IncidentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncident>> GetByTypeAsync(IncidentType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncident>> GetByReportedUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncident>> GetByAssignedToAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<SecurityIncident?> GetWithDetailsAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
