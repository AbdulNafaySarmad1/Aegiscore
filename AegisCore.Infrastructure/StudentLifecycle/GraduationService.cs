using System.Data;
using System.Text.Json;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.DegreeAudit;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.StudentLifecycle;

public sealed record GraduationProgress(Guid StudentId, string StudentName, string? Program,
    decimal Cgpa, int EarnedCredits, int RequiredCredits, bool Approved, IReadOnlyList<string> Blockers);

public sealed class GraduationService(AegisDbContext db)
{
    public static readonly string[] ClearanceDepartments = ["Library", "Finance", "Department"];

    public async Task<GraduationProgress> EvaluateAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == studentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");
        var blockers = new List<string>();
        var applicant = await db.ApplicantProfiles.SingleOrDefaultAsync(a => a.Id == student.AdmissionApplicantProfileId && a.ConvertedStudentId == studentId, ct);
        var program = applicant is null ? null : await db.DegreePrograms.SingleOrDefaultAsync(p => p.Id == applicant.ProgramId, ct);
        if (program is null) blockers.Add("An admitted degree program must be linked to this student.");
        var courses = await db.Courses.AsNoTracking().ToListAsync(ct);
        var enrollments = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId).ToListAsync(ct);
        var passing = enrollments.Where(e => AcademicRecordMath.IsPassing(e.Grade)).Select(e => e.CourseId).ToHashSet();
        var credits = courses.Where(c => passing.Contains(c.Id)).Sum(c => c.CreditHours);
        var cgpa = AcademicRecordMath.Cgpa(enrollments, courses);
        if (enrollments.Any(e => e.Grade == "F" && !passing.Contains(e.CourseId))) blockers.Add("Unresolved failed courses remain.");
        if (enrollments.Any(e => string.IsNullOrWhiteSpace(e.Grade) && e.EnrollmentStatus == EnrollmentStatus.Active)) blockers.Add("Active courses still require final grades.");
        // Credit transfer policies differ by institution; approval is fail-closed until
        // transfers are reconciled into the supported program audit.
        if (await db.CreditTransfers.AnyAsync(t => t.StudentId == studentId && t.IsApproved, ct))
            blockers.Add("Approved credit transfers require registrar reconciliation before this graduation workflow can be used.");
        if (program is not null)
        {
            if (credits < program.MinCreditHours) blockers.Add($"Earned credits {credits} are below {program.MinCreditHours}.");
            if (cgpa < program.MinCGPA) blockers.Add($"CGPA {cgpa:F2} is below {program.MinCGPA:F2}.");
            var requirements = await db.DegreeRequirements.Where(r => r.ProgramId == program.Id && r.IsRequired).ToListAsync(ct);
            if (requirements.Count == 0) blockers.Add("The program has no configured mandatory degree requirements.");
            foreach (var requirement in requirements)
            {
                var allowed = JsonSerializer.Deserialize<List<Guid>>(requirement.AllowedCourseIdsJson) ?? [];
                var met = requirement.RequirementType switch
                {
                    RequirementType.MinimumCredits => requirement.MinCredits > 0 && credits >= requirement.MinCredits,
                    RequirementType.MinimumCGPA => requirement.MinGradePoints.HasValue && cgpa >= requirement.MinGradePoints,
                    RequirementType.CourseCompletion => requirement.CourseId.HasValue && passing.Contains(requirement.CourseId.Value)
                        && (!requirement.MinGradePoints.HasValue || enrollments.Any(e => e.CourseId == requirement.CourseId && e.GpaPoints >= requirement.MinGradePoints && AcademicRecordMath.IsPassing(e.Grade))),
                    RequirementType.CourseGroup => requirement.CourseCount > 0 && allowed.Count(passing.Contains) >= requirement.CourseCount,
                    RequirementType.ElectiveCredits => requirement.MinCredits > 0 && allowed.Count > 0
                        && courses.Where(c => allowed.Contains(c.Id) && passing.Contains(c.Id)).Sum(c => c.CreditHours) >= requirement.MinCredits,
                    _ => false
                };
                if (!met) blockers.Add($"Requirement incomplete: {requirement.Name} ({requirement.Category}).");
            }
        }
        if (await HasOutstandingFeesAsync(studentId, ct)) blockers.Add("Outstanding or unreconciled fees remain.");
        var clearances = await db.StudentClearances.Where(c => c.StudentId == studentId).ToListAsync(ct);
        foreach (var department in ClearanceDepartments)
            if (!clearances.Any(c => c.Department == department && c.Status == ClearanceStatus.Cleared && c.ClearedByUserId != null && c.ClearedAt != null))
                blockers.Add($"{department} clearance is required.");
        var approved = student.EnrollmentStatus == EnrollmentStatus.Graduated
            && clearances.Any(c => c.Department == "Graduation" && c.Status == ClearanceStatus.Cleared && c.ClearedByUserId != null && c.ClearedAt != null);
        return new(studentId, $"{student.FirstName} {student.LastName}", program?.Name, Math.Round(cgpa, 2), credits, program?.MinCreditHours ?? 0, approved, blockers);
    }

    public async Task<bool> HasOutstandingFeesAsync(Guid studentId, CancellationToken ct = default)
    {
        var dues = await db.StudentFeeDues.Where(d => d.StudentId == studentId).ToListAsync(ct);
        var dueIds = dues.Select(d => d.Id).ToList();
        var payments = await db.StudentFeePayments.Where(p => dueIds.Contains(p.FeeDueId) && p.VerifiedBy != null && p.VerificationDate != null).ToListAsync(ct);
        if (dues.Any(d => d.TotalDue - d.ScholarshipDeduction > payments.Where(p => p.FeeDueId == d.Id).Sum(p => p.Amount))) return true;
        var demands = await db.FeeDemands.Where(d => d.StudentId == studentId).ToListAsync(ct);
        var demandIds = demands.Select(d => d.Id).ToList();
        var settled = await db.FinancePayments.Where(p => demandIds.Contains(p.FeeDemandId) && p.Status == "SETTLED" && p.SettledAt != null).ToListAsync(ct);
        if (demands.Any(d => d.TotalAmount > settled.Where(p => p.FeeDemandId == d.Id).Sum(p => p.Amount))) return true;
        var legacy = await db.StudentFees.Include(f => f.Payments).Where(f => f.StudentId == studentId).ToListAsync(ct);
        return legacy.Any(f => f.Amount > f.Payments.Sum(p => p.Amount));
    }

    public async Task ClearAsync(Guid studentId, string department, Guid actor, CancellationToken ct = default)
    {
        if (!ClearanceDepartments.Contains(department) || actor == Guid.Empty) throw new InvalidOperationException("Invalid clearance department or approver.");
        if (db.CurrentTenantId == Guid.Empty || db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Clearance requires a tenant and clean unit of work.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Students.AnyAsync(s => s.Id == studentId, ct)) throw new KeyNotFoundException("Student not found.");
        if (department == "Finance" && await HasOutstandingFeesAsync(studentId, ct)) throw new InvalidOperationException("Reconcile all fees before Finance clearance.");
        var clearance = await db.StudentClearances.FirstOrDefaultAsync(c => c.StudentId == studentId && c.Department == department, ct);
        if (clearance is null)
        {
            clearance = new StudentClearance { Id = Guid.NewGuid(), TenantId = db.CurrentTenantId, StudentId = studentId, Department = department };
            db.StudentClearances.Add(clearance);
        }
        if (clearance.Status == ClearanceStatus.Cleared && clearance.ClearedByUserId != null && clearance.ClearedAt != null) return;
        clearance.Status = ClearanceStatus.Cleared; clearance.ClearedByUserId = actor; clearance.ClearedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        });
    }

    public async Task<AlumniProfile> ApproveAsync(Guid studentId, Guid actor, CancellationToken ct = default)
    {
        if (actor == Guid.Empty || db.CurrentTenantId == Guid.Empty) throw new InvalidOperationException("An authenticated tenant approver is required.");
        if (db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Graduation approval requires a clean unit of work.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var progress = await EvaluateAsync(studentId, ct);
            var existing = await db.AlumniProfiles.SingleOrDefaultAsync(a => a.UserId == studentId, ct);
            if (progress.Approved && existing is not null) return existing;
            if (progress.Blockers.Count > 0) throw new InvalidOperationException(string.Join(" ", progress.Blockers));
            var student = await db.Students.SingleAsync(s => s.Id == studentId, ct);
            if (!await db.Users.AnyAsync(u => u.Id == studentId, ct)) throw new InvalidOperationException("Linked student account is missing.");
            db.Entry(student).Property(s => s.EnrollmentStatus).CurrentValue = EnrollmentStatus.Graduated;
            var approval = new StudentClearance { Id = Guid.NewGuid(), TenantId = db.CurrentTenantId, StudentId = studentId,
                Department = "Graduation", Status = ClearanceStatus.Cleared, ClearedByUserId = actor, ClearedAt = DateTime.UtcNow };
            db.StudentClearances.Add(approval);
            var alumni = new AlumniProfile { Id = studentId, UserId = studentId, TenantId = db.CurrentTenantId,
                GraduationYear = DateTime.UtcNow.Year, AlumniId = $"ALM-{DateTime.UtcNow.Year}-{studentId:N}" };
            db.AlumniProfiles.Add(alumni);
            var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "Alumni", ct);
            if (role is null) { role = new Role { Id = Guid.NewGuid(), TenantId = db.CurrentTenantId, Name = "Alumni" }; db.Roles.Add(role); }
            db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = db.CurrentTenantId, UserId = studentId, RoleId = role.Id, AssignedByUserId = actor });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return alumni;
        });
    }
}
