using AegisCore.Domain;
namespace AegisCore.Contracts.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    ICourseRepository Courses { get; }
    IEnrollmentRepository Enrollments { get; }
    IAttendanceRepository Attendances { get; }
    IExamSeatingRepository ExamSeatings { get; }
    IRepository<Domain.Entities.Batch> Batches { get; }
    IRepository<Domain.Entities.Department> Departments { get; }
    IRepository<Domain.Entities.Faculty> Faculties { get; }
    IRepository<Domain.Entities.CourseAssignment> CourseAssignments { get; }
    IRepository<Domain.Entities.ExamSchedule> ExamSchedules { get; }
    IRepository<Domain.Entities.Result> Results { get; }
    IRepository<Domain.Entities.Timetable> Timetables { get; }
    IRepository<Domain.Entities.CloPlomapping> CloPlomappings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
