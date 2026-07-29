using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class CalibrationRecordRepository : Repository<CalibrationRecord>, ICalibrationRecordRepository
{
    public CalibrationRecordRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CalibrationRecord>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.LabEquipmentId == equipmentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalibrationRecord>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.LabEquipment)
            .Where(c => c.NextCalibrationDue < DateTime.UtcNow && c.Status == CalibrationStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task<CalibrationRecord?> GetLatestAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.LabEquipmentId == equipmentId)
            .OrderByDescending(c => c.CalibrationDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
