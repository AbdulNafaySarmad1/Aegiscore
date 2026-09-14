using AegisCore.Application.StudentLifecycle;
using AegisCore.API.Authorization;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/student-lifecycle")]
[Authorize]
public class StudentLifecycleController : ControllerBase
{
    private readonly AegisDbContext _db;
    private readonly IStudentLifecycleService _service;

    public StudentLifecycleController(AegisDbContext db, IStudentLifecycleService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet("placement/eligible")]
    public IActionResult Eligible([FromQuery] decimal cgpa, [FromQuery] int completedCredits, [FromQuery] int requiredCredits, [FromQuery] int failedCourses, [FromQuery] decimal minCgpa = 2.5m)
        => Ok(new { eligible = _service.IsPlacementEligible(cgpa, completedCredits, requiredCredits, failedCourses, minCgpa) });

    [HttpPost("alumni")]
    [RequirePermission("Graduation", "Approve")]
    public async Task<IActionResult> UpsertAlumni([FromBody] AlumniProfile profile, CancellationToken ct)
    {
        // Academic status and an attributable approval must exist before a profile can
        // be created. Never accept graduation or tenant identity from the request body.
        var tenantId = _db.CurrentTenantId;
        if (tenantId == Guid.Empty) return Forbid();
        var student = await _db.Students.SingleOrDefaultAsync(s => s.Id == profile.UserId && s.TenantId == tenantId, ct);
        if (student is null || !await _db.Users.AnyAsync(u => u.Id == profile.UserId && u.TenantId == tenantId, ct)) return NotFound();
        var approval = await _db.StudentClearances
            .Where(c => c.TenantId == tenantId && c.StudentId == student.Id && c.Department == "Graduation"
                && c.Status == ClearanceStatus.Cleared && c.ClearedByUserId != null && c.ClearedAt != null)
            .OrderByDescending(c => c.ClearedAt).FirstOrDefaultAsync(ct);
        if (student.EnrollmentStatus != EnrollmentStatus.Graduated || approval is null)
            return Conflict(new { error = "Approved graduation is required before alumni creation." });
        var existing = await _db.AlumniProfiles.SingleOrDefaultAsync(a => a.TenantId == tenantId && a.UserId == student.Id, ct);
        if (existing is not null) return Ok(existing);
        // A stable primary key also prevents concurrent duplicate inserts.
        profile.Id = student.Id;
        profile.TenantId = tenantId;
        profile.GraduationYear = approval.ClearedAt!.Value.Year;
        profile.CreatedAt = DateTime.UtcNow;
        profile.AlumniId = $"ALM-{profile.GraduationYear}-{student.Id:N}";
        _db.AlumniProfiles.Add(profile);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            _db.Entry(profile).State = EntityState.Detached;
            var concurrent = await _db.AlumniProfiles.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == student.Id && a.TenantId == tenantId && a.UserId == student.Id, ct);
            if (concurrent is null) throw;
            return Ok(concurrent);
        }
        return Ok(profile);
    }

    [HttpGet("alumni/me")]
    public async Task<IActionResult> MyAlumniProfile(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var subject)) return Forbid();
        var profile = await _db.AlumniProfiles.AsNoTracking().SingleOrDefaultAsync(a => a.UserId == subject && a.TenantId == _db.CurrentTenantId, ct);
        return profile is null ? NotFound(new { error = "No approved alumni profile exists." }) : Ok(profile);
    }

    [HttpPost("alumni/{alumniProfileId:guid}/donations")]
    public async Task<IActionResult> Donation(Guid alumniProfileId, [FromBody] AlumniDonation donation, CancellationToken ct)
    {
        if (!IsPrivileged() && !await OwnsAlumniAsync(alumniProfileId, ct)) return Forbid();
        donation.AlumniProfileId = alumniProfileId;
        donation.ReceiptNumber = _service.CreateReceiptNumber(donation.DonationDate == default ? DateTime.UtcNow : donation.DonationDate, donation.Id == default ? Guid.NewGuid() : donation.Id);
        _db.AlumniDonations.Add(donation);
        await _db.SaveChangesAsync(ct);
        return Ok(donation);
    }

    [HttpPost("societies")]
    public async Task<IActionResult> Society([FromBody] StudentSociety society, CancellationToken ct)
    {
        _db.StudentSocieties.Add(society);
        await _db.SaveChangesAsync(ct);
        return Ok(society);
    }

    [HttpPost("activities/{activityId:guid}/participation")]
    public async Task<IActionResult> Participation(Guid activityId, [FromBody] ActivityParticipation participation, CancellationToken ct)
    {
        participation.ActivityId = activityId;
        participation.PointsEarned = _service.CalculateActivityPoints(participation.ParticipationRole, participation.ParticipationStatus == "completed");
        _db.ActivityParticipations.Add(participation);
        await _db.SaveChangesAsync(ct);
        return Ok(participation);
    }

    [HttpGet("health-records/{studentId:guid}")]
    public async Task<IActionResult> HealthRecord(Guid studentId, CancellationToken ct)
    {
        if (!IsPrivileged() && !Owns(studentId)) return Forbid();
        return Ok(await _db.StudentHealthRecords.AsNoTracking().FirstOrDefaultAsync(r => r.StudentId == studentId, ct));
    }

    private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Faculty");
    private bool Owns(Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var subject) && subject == id;
    private Task<bool> OwnsAlumniAsync(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var subject)) return Task.FromResult(false);
        return _db.AlumniProfiles.AnyAsync(a => a.Id == id && a.UserId == subject, ct);
    }
}
