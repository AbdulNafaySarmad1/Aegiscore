using AegisCore.Application.StudentLifecycle;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.StudentLifecycle;

public sealed class ForeignStudentService(AegisDbContext db, ILogger<ForeignStudentService> logger) : IForeignStudentService
{
    private static readonly int[] AlertDays = [90, 60, 30, 14, 7];

    public async Task<IReadOnlyList<ExpiryAlertDto>> GetDocumentExpiryAlertsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var records = await db.Set<ForeignStudentRecord>().Where(r => r.TenantId == tenantId).ToListAsync(ct);
        var alerts = new List<ExpiryAlertDto>();
        foreach (var r in records)
        {
            foreach (var (docType, expiryDate) in GetDocuments(r))
            {
                var daysRemaining = (int)(expiryDate - DateTimeOffset.UtcNow).TotalDays;
                if (daysRemaining > 90) continue;
                var urgency = daysRemaining switch { <= 0 => "expired", <= 7 => "emergency", <= 14 => "critical", <= 30 => "urgent", <= 60 => "action", _ => "reminder" };
                alerts.Add(new ExpiryAlertDto(r.StudentId, r.Nationality, docType, expiryDate, daysRemaining, urgency, daysRemaining <= 0));
            }
        }
        return alerts.OrderBy(a => a.DaysRemaining).ToList();
    }

    public async Task<MoraComplianceDto> GetMoraComplianceAsync(Guid semesterId, CancellationToken ct = default)
    {
        var submissions = await db.Set<MoraSubmission>().Where(s => s.SemesterId == semesterId).ToListAsync(ct);
        var lastSubmitted = submissions.Where(s => s.Status == "submitted").MaxBy(s => s.SubmittedAt);
        var foreignStudents = await db.Set<ForeignStudentRecord>().CountAsync(ct);
        var registered = await db.Set<ForeignStudentRecord>().CountAsync(f => f.MoraRegistrationStatus == "registered", ct);
        return new MoraComplianceDto(semesterId, foreignStudents, registered, foreignStudents - registered, lastSubmitted?.Status ?? "draft", lastSubmitted?.SubmittedAt, lastSubmitted?.MoraReferenceNumber);
    }

    public async Task<EnrollmentLetterDto> GenerateEnrollmentLetterAsync(Guid studentId, string purpose, CancellationToken ct = default)
    {
        var record = await db.Set<ForeignStudentRecord>().FirstOrDefaultAsync(r => r.StudentId == studentId, ct);
        var letter = new EnrollmentLetter
        {
            Id = Guid.NewGuid(), StudentId = studentId, Purpose = purpose,
            PdfUrl = $"/letters/enrollment-{studentId}-{DateTimeOffset.UtcNow:yyyyMMdd}.pdf",
            QrVerificationCode = Guid.NewGuid().ToString("N")[..16], IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMonths(3), IssuedBy = Guid.NewGuid(), TenantId = record?.TenantId ?? Guid.NewGuid()
        };
        db.Set<EnrollmentLetter>().Add(letter);
        await db.SaveChangesAsync(ct);
        return new EnrollmentLetterDto(letter.Id, studentId, $"Student-{studentId.ToString()[..8]}", record?.Nationality ?? "Unknown", "BS", 2, letter.PdfUrl, letter.QrVerificationCode, letter.IssuedAt);
    }

    public async Task<VisaRenewalWorkflowDto> InitiateVisaRenewalAsync(Guid studentId, CancellationToken ct = default)
    {
        var record = await db.Set<ForeignStudentRecord>().FirstOrDefaultAsync(r => r.StudentId == studentId, ct) ?? throw new KeyNotFoundException("Foreign student record not found.");
        var workflow = new VisaRenewalWorkflow
        {
            Id = Guid.NewGuid(), ForeignStudentRecordId = record.Id, Status = "draft",
            ApplicationDeadline = record.VisaExpiry.AddDays(-60), TenantId = record.TenantId
        };
        db.Set<VisaRenewalWorkflow>().Add(workflow);
        await db.SaveChangesAsync(ct);
        return new VisaRenewalWorkflowDto(workflow.Id, studentId, workflow.Status, workflow.ApplicationDeadline, ["Passport copy", "Enrollment letter", "Bank statement", "MORA registration proof", "Health certificate"], null, null);
    }

    public async Task<IReadOnlyList<ComplianceBreachDto>> GetComplianceBreachesAsync(Guid tenantId, CancellationToken ct = default)
    {
        var breaches = await db.Set<ComplianceBreach>().Where(b => b.TenantId == tenantId && b.Status == "open").ToListAsync(ct);
        return breaches.Select(b => new ComplianceBreachDto(b.Id, b.ForeignStudentRecordId, b.BreachType, b.Description, b.Status, b.OccurredAt, b.ResolutionType)).ToList();
    }

    public async Task<IoDashboardDto> GetIoDashboardAsync(Guid tenantId, CancellationToken ct = default)
    {
        var students = await db.Set<ForeignStudentRecord>().Where(r => r.TenantId == tenantId).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return new IoDashboardDto(students.Count, students.GroupBy(s => s.Nationality).ToDictionary(g => g.Key, g => g.Count()), students.Count(s => s.ComplianceStatus == "fully_compliant"), students.Count(s => s.ComplianceStatus == "action_required"), students.Count(s => s.ComplianceStatus == "non_compliant"), students.Count(s => s.VisaExpiry <= now.AddDays(30) && s.VisaExpiry > now), students.Count(s => s.VisaExpiry <= now.AddDays(60) && s.VisaExpiry > now.AddDays(30)), students.Count(s => s.VisaExpiry <= now.AddDays(90) && s.VisaExpiry > now.AddDays(60)), "draft");
    }

    private static (string Type, DateTimeOffset Expiry)[] GetDocuments(ForeignStudentRecord r) => [( "Passport", r.PassportExpiry), ("Visa", r.VisaExpiry)];
}
