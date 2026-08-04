using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IVehiclePassRepository : IRepository<VehiclePass>
{
    Task<IReadOnlyList<VehiclePass>> GetByOwnerEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehiclePass>> GetByOwnerStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehiclePass>> GetByVisitorAsync(Guid visitorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehiclePass>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task<VehiclePass?> GetByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default);
}
