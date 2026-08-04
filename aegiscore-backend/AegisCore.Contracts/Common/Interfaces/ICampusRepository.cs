using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ICampusRepository : IRepository<Campus>
{
    Task<Campus?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campus>> GetByStatusAsync(CampusStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campus>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IBuildingRepository : IRepository<Building>
{
    Task<IReadOnlyList<Building>> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default);
}

public interface IFloorRepository : IRepository<Floor>
{
    Task<IReadOnlyList<Floor>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
}

public interface IRoomRepository : IRepository<Room>
{
    Task<IReadOnlyList<Room>> GetByFloorAsync(Guid floorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default);
}

public interface ICampusConfigurationRepository : IRepository<CampusConfiguration>
{
    Task<CampusConfiguration?> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default);
}
