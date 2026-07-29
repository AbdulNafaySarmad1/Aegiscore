using AegisCore.Domain.Entities;
using CampusEntity = AegisCore.Domain.Entities.Campus;

namespace AegisCore.Application.Common.Interfaces;

public interface ICampusRepository : IRepository<CampusEntity>
{
    Task<CampusEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusEntity>> GetByStatusAsync(CampusStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusEntity>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
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
