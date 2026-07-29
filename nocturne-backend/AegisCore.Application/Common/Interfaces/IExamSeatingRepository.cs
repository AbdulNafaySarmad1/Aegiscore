using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IExamSeatingRepository : IRepository<ExamSeating>
{
    Task<ExamSeating?> GetByExamAndStudentAsync(Guid examScheduleId, Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamSeating>> GetAllSeatsForExamAsync(Guid examScheduleId, CancellationToken cancellationToken = default);
}
