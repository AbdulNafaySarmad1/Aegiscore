using AegisCore.Application.StudentLifecycle;
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
    public async Task<IActionResult> UpsertAlumni([FromBody] AlumniProfile profile, CancellationToken ct)
    {
        if (!IsPrivileged() && !Owns(profile.UserId)) return Forbid();
        profile.AlumniId = string.IsNullOrWhiteSpace(profile.AlumniId) ? $"ALM-{profile.GraduationYear}-{profile.UserId.ToString("N")[..6].ToUpperInvariant()}" : profile.AlumniId;
        _db.AlumniProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return Ok(profile);
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
