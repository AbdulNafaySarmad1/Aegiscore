using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class VehiclePassRepository : Repository<VehiclePass>, IVehiclePassRepository
{
    public VehiclePassRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<VehiclePass>> GetByOwnerEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.OwnerEmployeeId == employeeId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VehiclePass>> GetByOwnerStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.OwnerStudentId == studentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VehiclePass>> GetByVisitorAsync(Guid visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.VisitorId == visitorId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VehiclePass>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.ExpiryDate <= beforeDate && v.Status == VehiclePassStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<VehiclePass?> GetByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber && v.Status == VehiclePassStatus.Active, cancellationToken);
    }
}
