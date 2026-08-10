using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/admissions")]
[Authorize]
public sealed class S2AdmissionsController(
    AegisDbContext db,
    IStorageProvider storage,
    IMeritCalculationService merit,
    ISeatAllocationService allocation,
    IIBCCEquivalenceService ibcc,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("enquiries")]
    public async Task<ActionResult<AdmissionLead>> SubmitEnquiry([FromBody] EnquiryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest("Name and phone are required.");
        }

        if (!Enum.TryParse<LeadSource>(request.Source, true, out var source))
        {
            return BadRequest("Source is invalid.");
        }

        var lead = new AdmissionLead
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = request.Phone.Trim(),
            ProgramInterest = request.ProgramInterest?.Trim(),
            Source = source,
            Status = LeadStatus.New,
            TenantId = Guid.Empty
        };
        db.AdmissionLeads.Add(lead);
        await db.SaveChangesAsync(ct);
        return Created($"/api/admissions/enquiries/{lead.Id}", lead);
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpGet("enquiries")]
    public async Task<IReadOnlyList<AdmissionLead>> ListEnquiries([FromQuery] LeadStatus? status, CancellationToken ct)
    {
        var query = db.AdmissionLeads.AsQueryable();
        if (status is not null) query = query.Where(l => l.Status == status);
        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpPatch("enquiries/{id:guid}/status")]
    public async Task<IActionResult> UpdateEnquiryStatus(Guid id, [FromBody] EnquiryStatusRequest request, CancellationToken ct)
    {
        var lead = await db.AdmissionLeads.FindAsync([id], ct);
        if (lead is null) return NotFound();
        if (!Enum.TryParse<LeadStatus>(request.Status, true, out var status)) return BadRequest("Status is invalid.");
        lead.Status = status;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("applications")]
    public async Task<ActionResult<object>> Submit([FromBody] AdmissionApplicationRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var applicantUserId)
            || applicantUserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var tenantId = db.GetRequiredTenantId();
        if (!await db.Users.AsNoTracking().AnyAsync(user => user.Id == applicantUserId && user.TenantId == tenantId, ct))
        {
            return Unauthorized();
        }

        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-14))
            || request.DateOfBirth == default)
        {
            return BadRequest("Applicant date of birth is not valid for university admission.");
        }

        if (string.IsNullOrWhiteSpace(request.StudentName) || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Cnic))
        {
            return BadRequest("Applicant name, email, phone, and CNIC are required.");
        }

        if (!Regex.IsMatch(request.Cnic, "^\\d{5}-\\d{7}-\\d$"))
        {
            return BadRequest("CNIC must use the format 00000-0000000-0.");
        }

        if (request.MatricPercentage is < 0 or > 100 || request.FscPercentage is < 0 or > 100 || request.EntryTestScore is < 0)
        {
            return BadRequest("Admission scores are invalid.");
        }

        if (request.QuotaType is not ("open_merit" or "self_finance" or "hafiz" or "sports" or "pmc"))
        {
            return BadRequest("Quota type is invalid.");
        }

        if (!await db.DegreePrograms.AnyAsync(program => program.Id == request.ProgramId && program.TenantId == tenantId, ct))
        {
            return BadRequest("Program was not found.");
        }

        if (await db.AdmissionApplications.IgnoreQueryFilters().AnyAsync(application => application.Cnic == request.Cnic, ct))
        {
            return Conflict("An application already exists for this CNIC.");
        }

        var now = DateTimeOffset.UtcNow;
        var application = new AdmissionApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = applicantUserId,
            StudentName = request.StudentName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Cnic = request.Cnic,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender?.Trim(),
            ProgramId = request.ProgramId,
            QuotaType = request.QuotaType,
            MatricPercentage = request.MatricPercentage,
            FscPercentage = request.FscPercentage,
            EntryTestScore = request.EntryTestScore,
            IbccEquivalenceApplied = request.IbccEquivalenceApplied,
            ApplicationStatus = "submitted",
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AdmissionApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = application.Id }, new
        {
            application,
            trackingToken = TrackingToken(application.Id)
        });
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpGet("applications/{id:guid}")]
    public async Task<ActionResult<AdmissionApplication>> Get(Guid id, CancellationToken ct)
        => await db.AdmissionApplications.Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == id, ct) is { } app ? app : NotFound();

    [HttpPost("applications/{id:guid}/documents")]
    public async Task<ActionResult<ApplicationDocument>> UploadDocument(Guid id, IFormFile file, [FromForm] string documentType, [FromQuery] string trackingToken, CancellationToken ct)
    {
        if (!HasTrackingToken(id, trackingToken)) return NotFound();

        if (file.Length is <= 0 or > 5 * 1024 * 1024)
        {
            return BadRequest("Document must be between 1 byte and 5MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".jpg" and not ".jpeg" and not ".png")
        {
            return BadRequest("Only PDF, JPG, and PNG files are allowed.");
        }

        if (!await db.AdmissionApplications.AnyAsync(a => a.Id == id, ct))
        {
            return NotFound();
        }

        if (documentType is not ("matric_cert" or "fsc_cert" or "cnic" or "entry_test_result" or "quota_proof"))
        {
            return BadRequest("Document type is invalid.");
        }

        await using var probe = file.OpenReadStream();
        var header = new byte[8];
        var read = await probe.ReadAsync(header, ct);
        if (!ValidFileSignature(extension, header, read))
        {
            return BadRequest("File content does not match its extension.");
        }

        await using var stream = file.OpenReadStream();
        var path = await storage.UploadAsync($"admissions/{id}/{Guid.NewGuid()}{extension}", stream, file.ContentType, ct);
        var document = new ApplicationDocument { Id = Guid.NewGuid(), ApplicationId = id, DocumentType = documentType, FilePath = path };
        db.ApplicationDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpPost("applications/{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyApplicationRequest request, CancellationToken ct)
    {
        var app = await db.AdmissionApplications.Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (app is null) return NotFound();

        app.ApplicationStatus = request.Approved ? "verified" : "rejected";
        app.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var doc in app.Documents)
        {
            doc.VerificationStatus = request.Approved ? "verified" : "rejected";
            doc.VerifiedBy = request.ActorId;
            doc.VerifiedAt = DateTimeOffset.UtcNow;
            doc.RejectionReason = request.Approved ? null : request.Reason;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpPost("merit-lists")]
    public async Task<ActionResult<MeritList>> GenerateMeritList([FromBody] GenerateMeritListRequest request, CancellationToken ct)
    {
        if (request.RoundNumber < 1 || request.TotalSeats < 0 || request.QuotaType is not ("open_merit" or "self_finance" or "hafiz" or "sports" or "pmc"))
        {
            return BadRequest("Merit list parameters are invalid.");
        }

        if (!await db.DegreePrograms.AnyAsync(p => p.Id == request.ProgramId, ct)
            || !await db.Semesters.AnyAsync(s => s.Id == request.SemesterId, ct))
        {
            return BadRequest("Program or semester was not found.");
        }

        var results = await merit.GenerateMeritListAsync(request.ProgramId, request.SemesterId, request.QuotaType, ct);
        var list = new MeritList
        {
            Id = Guid.NewGuid(),
            ProgramId = request.ProgramId,
            SemesterId = request.SemesterId,
            QuotaType = request.QuotaType,
            RoundNumber = request.RoundNumber,
            TotalSeats = request.TotalSeats,
            GeneratedBy = request.ActorId,
            Entries = results.Select(r => new MeritListEntry
            {
                Id = Guid.NewGuid(),
                ApplicationId = r.ApplicationId,
                MeritRank = r.MeritRank,
                MeritScore = r.MeritScore,
                MatricContribution = r.Explanation.MatricContribution,
                FscContribution = r.Explanation.FscContribution,
                EntryTestContribution = r.Explanation.EntryTestContribution
            }).ToList()
        };

        db.MeritLists.Add(list);
        await db.SaveChangesAsync(ct);
        return list;
    }

    [Authorize(Roles = "Admin,SuperAdmin,AdmissionsHead")]
    [HttpPost("merit-lists/{id:guid}/allocate")]
    public async Task<IReadOnlyList<SeatAllocation>> Allocate(Guid id, CancellationToken ct) => await allocation.AllocateAsync(id, ct);

    [HttpGet("ibcc")]
    public object Ibcc(decimal oLevelScore, decimal aLevelScore) => new
    {
        matric = ibcc.ConvertOLevelToMatric(oLevelScore),
        fsc = ibcc.ConvertALevelToFsc(aLevelScore)
    };

    [HttpGet("applications/{id:guid}/track/{token}")]
    public async Task<IActionResult> Track(Guid id, string token, CancellationToken ct)
    {
        if (!HasTrackingToken(id, token))
        {
            return NotFound();
        }

        var application = await db.AdmissionApplications
            .Include(a => a.Documents)
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.ApplicationStatus, a.SubmittedAt, Documents = a.Documents.Select(d => new { d.DocumentType, d.VerificationStatus, d.RejectionReason }) })
            .FirstOrDefaultAsync(ct);
        return application is null ? NotFound() : Ok(application);
    }

    private string TrackingToken(Guid applicationId)
    {
        var secret = configuration["Admissions:TrackingTokenSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return string.Empty;
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(applicationId.ToString("N")))).ToLowerInvariant();
    }

    private bool HasTrackingToken(Guid applicationId, string? token)
    {
        var expected = TrackingToken(applicationId);
        return expected.Length > 0 && !string.IsNullOrWhiteSpace(token)
            && token.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(token));
    }

    private static bool ValidFileSignature(string extension, byte[] header, int length)
        => extension == ".pdf" && length >= 4 && header[..4].SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 })
           || extension is ".jpg" or ".jpeg" && length >= 3 && header[..3].SequenceEqual(new byte[] { 0xff, 0xd8, 0xff })
           || extension == ".png" && length >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
}

public sealed record VerifyApplicationRequest(bool Approved, Guid ActorId, string? Reason);
public sealed record GenerateMeritListRequest(Guid ProgramId, Guid SemesterId, string QuotaType, int RoundNumber, int TotalSeats, Guid ActorId);
public sealed record EnquiryRequest(string Name, string? Email, string Phone, string? ProgramInterest, string Source);
public sealed record EnquiryStatusRequest(string Status);
public sealed record AdmissionApplicationRequest(
    string StudentName,
    string Email,
    string Phone,
    string Cnic,
    DateOnly DateOfBirth,
    string? Gender,
    Guid ProgramId,
    string QuotaType,
    decimal? MatricPercentage,
    decimal? FscPercentage,
    int? EntryTestScore,
    bool IbccEquivalenceApplied);
