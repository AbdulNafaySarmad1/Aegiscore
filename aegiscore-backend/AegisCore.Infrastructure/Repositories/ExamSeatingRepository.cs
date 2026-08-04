using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Repositories;

public class ExamSeatingRepository : Repository<ExamSeating>, IExamSeatingRepository
{
    public ExamSeatingRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<ExamSeating?> GetByExamAndStudentAsync(Guid examScheduleId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(es => es.ExamScheduleId == examScheduleId && es.StudentId == studentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamSeating>> GetAllSeatsForExamAsync(Guid examScheduleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(es => es.ExamScheduleId == examScheduleId)
            .Include(es => es.Student)
            .OrderBy(es => es.SeatNumber)
            .ToListAsync(cancellationToken);
    }
}
