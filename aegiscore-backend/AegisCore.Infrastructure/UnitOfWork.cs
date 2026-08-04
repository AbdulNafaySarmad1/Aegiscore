using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;

namespace AegisCore.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AegisDbContext _context;
    private IStudentRepository? _students;
    private ICourseRepository? _courses;
    private IEnrollmentRepository? _enrollments;
    private IAttendanceRepository? _attendances;
    private IExamSeatingRepository? _examSeatings;
    private IRepository<Domain.Entities.Batch>? _batches;
    private IRepository<Domain.Entities.Department>? _departments;
    private IRepository<Domain.Entities.Faculty>? _faculties;
    private IRepository<Domain.Entities.CourseAssignment>? _courseAssignments;
    private IRepository<Domain.Entities.ExamSchedule>? _examSchedules;
    private IRepository<Domain.Entities.Result>? _results;
    private IRepository<Domain.Entities.Timetable>? _timetables;
    private IRepository<Domain.Entities.CloPlomapping>? _cloPlomappings;

    public UnitOfWork(AegisDbContext context)
    {
        _context = context;
    }

    public IStudentRepository Students =>
        _students ??= new StudentRepository(_context);

    public ICourseRepository Courses =>
        _courses ??= new CourseRepository(_context);

    public IEnrollmentRepository Enrollments =>
        _enrollments ??= new EnrollmentRepository(_context);

    public IAttendanceRepository Attendances =>
        _attendances ??= new AttendanceRepository(_context);

    public IExamSeatingRepository ExamSeatings =>
        _examSeatings ??= new ExamSeatingRepository(_context);

    public IRepository<Domain.Entities.Batch> Batches =>
        _batches ??= new Repository<Domain.Entities.Batch>(_context);

    public IRepository<Domain.Entities.Department> Departments =>
        _departments ??= new Repository<Domain.Entities.Department>(_context);

    public IRepository<Domain.Entities.Faculty> Faculties =>
        _faculties ??= new Repository<Domain.Entities.Faculty>(_context);

    public IRepository<Domain.Entities.CourseAssignment> CourseAssignments =>
        _courseAssignments ??= new Repository<Domain.Entities.CourseAssignment>(_context);

    public IRepository<Domain.Entities.ExamSchedule> ExamSchedules =>
        _examSchedules ??= new Repository<Domain.Entities.ExamSchedule>(_context);

    public IRepository<Domain.Entities.Result> Results =>
        _results ??= new Repository<Domain.Entities.Result>(_context);

    public IRepository<Domain.Entities.Timetable> Timetables =>
        _timetables ??= new Repository<Domain.Entities.Timetable>(_context);

    public IRepository<Domain.Entities.CloPlomapping> CloPlomappings =>
        _cloPlomappings ??= new Repository<Domain.Entities.CloPlomapping>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
