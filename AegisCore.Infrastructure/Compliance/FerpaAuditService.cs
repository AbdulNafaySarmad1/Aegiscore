using System.Text;
using AegisCore.Application.Compliance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Compliance;

public sealed class FerpaAuditService(AegisDbContext db, IPqcCryptographyService encryption) : IFerpaAuditService
{
    public async Task<FerpaAccessDecision> AuthorizeAndAuditAsync(
        FerpaAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = db.CurrentTenantId;
        if (tenantId == Guid.Empty) throw new InvalidOperationException("Tenant context is required for FERPA auditing.");

        var roles = request.UserRoles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var isAdmin = roles.Any(role => role is "Admin" or "SuperAdmin");
        var targets = await ResolveTargetsAsync(request, cancellationToken);
        var justified = isAdmin || await HasRelationshipAsync(request, targets, cancellationToken);
        var targetStudents = targets.StudentIds.Count == 0 ? [Guid.Empty] : targets.StudentIds;

        foreach (var studentId in targetStudents)
        {
            var entry = new FerpaAuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = request.UserId,
                UserRole = roles.Length == 0 ? "Unknown" : string.Join(",", roles),
                ResourceType = request.ResourceType,
                ResourceId = targets.ResourceId,
                StudentId = studentId,
                Action = request.Action,
                AccessedAt = DateTime.UtcNow,
                Justified = justified
            };
            if (!string.IsNullOrWhiteSpace(request.IpAddress))
            {
                var plaintext = Encoding.UTF8.GetBytes(request.IpAddress);
                var aad = Encoding.UTF8.GetBytes($"{tenantId:N}:{entry.Id:N}:ferpa-ip");
                entry.SetEncryptedIpAddress(request.IpAddress, encryption.Encrypt(plaintext, aad));
            }

            db.FerpaAuditLogs.Add(entry);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new FerpaAccessDecision(justified, targetStudents.Count);
    }

    private async Task<(Guid ResourceId, IReadOnlyList<Guid> StudentIds)> ResolveTargetsAsync(
        FerpaAccessRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IdentifierName.Equals("courseId", StringComparison.OrdinalIgnoreCase))
        {
            var students = await db.Enrollments.AsNoTracking()
                .Where(enrollment => enrollment.CourseId == request.Identifier)
                .Select(enrollment => enrollment.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return (request.Identifier, students);
        }

        if (request.ResourceType.Equals("Admission", StringComparison.OrdinalIgnoreCase))
            return (request.Identifier, []);

        if (request.IdentifierName.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            if (request.ResourceType.Equals("Attendance", StringComparison.OrdinalIgnoreCase))
            {
                var studentId = await db.Attendances.AsNoTracking()
                    .Where(attendance => attendance.Id == request.Identifier)
                    .Select(attendance => (Guid?)attendance.Enrollment.StudentId)
                    .SingleOrDefaultAsync(cancellationToken);
                return (request.Identifier, studentId is null ? [] : [studentId.Value]);
            }

            if (request.ResourceType.Equals("Transcript", StringComparison.OrdinalIgnoreCase))
            {
                var studentId = await db.Transcripts.AsNoTracking()
                    .Where(transcript => transcript.Id == request.Identifier)
                    .Select(transcript => (Guid?)transcript.StudentId)
                    .SingleOrDefaultAsync(cancellationToken);
                return (request.Identifier, studentId is null ? [] : [studentId.Value]);
            }
        }

        return (request.Identifier, request.Identifier == Guid.Empty ? [] : [request.Identifier]);
    }

    private async Task<bool> HasRelationshipAsync(
        FerpaAccessRequest request,
        (Guid ResourceId, IReadOnlyList<Guid> StudentIds) targets,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty) return false;

        if (request.ResourceType.Equals("Admission", StringComparison.OrdinalIgnoreCase))
        {
            var userEmail = await db.Users.AsNoTracking()
                .Where(user => user.Id == request.UserId)
                .Select(user => user.Email)
                .SingleOrDefaultAsync(cancellationToken);
            return userEmail is not null && await db.AdmissionApplications.AsNoTracking()
                .AnyAsync(application => application.Id == request.Identifier && application.Email == userEmail, cancellationToken);
        }

        if (targets.StudentIds.Count == 0) return false;
        if (targets.StudentIds.All(studentId => studentId == request.UserId)) return true;

        var facultyId = request.UserId;
        if (request.IdentifierName.Equals("courseId", StringComparison.OrdinalIgnoreCase))
        {
            return await db.CourseAssignments.AsNoTracking().AnyAsync(
                assignment => assignment.CourseId == request.Identifier && assignment.FacultyId == facultyId,
                cancellationToken);
        }

        var studentIds = targets.StudentIds.ToArray();
        return await db.CourseAssignments.AsNoTracking().AnyAsync(
            assignment => assignment.FacultyId == facultyId
                && db.Enrollments.Any(enrollment =>
                    enrollment.CourseId == assignment.CourseId && studentIds.Contains(enrollment.StudentId)),
            cancellationToken);
    }
}
