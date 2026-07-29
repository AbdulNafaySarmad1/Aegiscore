using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Governance;

public sealed class GovernanceService(AegisDbContext db, IConfiguration configuration, ILogger<GovernanceService> logger) : IGovernanceService
{
    public int NormalizeReplicationLag(int? hours) => Math.Clamp(hours ?? 12, 1, 72);

    public string DetermineGradeApprovalThreshold(int originalMarks, int overrideMarks)
    {
        if (originalMarks < 0 || overrideMarks < 0) throw new ArgumentOutOfRangeException(nameof(originalMarks));
        if (originalMarks == 0) return overrideMarks <= 5 ? "hod" : overrideMarks <= 15 ? "vc" : "chancellor";

        var deltaPercent = Math.Abs(overrideMarks - originalMarks) * 100m / originalMarks;
        return deltaPercent <= 5m ? "hod" : deltaPercent <= 15m ? "vc" : "chancellor";
    }

    public decimal CalculateDynamicFee(int creditHours, decimal perCreditHour, decimal scholarshipDeduction, decimal overrideDeduction)
    {
        if (creditHours < 0) throw new ArgumentOutOfRangeException(nameof(creditHours));
        if (perCreditHour < 0 || scholarshipDeduction < 0 || overrideDeduction < 0) throw new ArgumentOutOfRangeException(nameof(perCreditHour));

        var gross = creditHours * perCreditHour;
        return Math.Max(0m, gross - scholarshipDeduction - overrideDeduction);
    }

    public bool IsSectionCapacityAllowed(int regularCount, int examCount, int regularCapacity, int examCapacity)
        => regularCount >= 0 && examCount >= 0 && regularCapacity > 0 && examCapacity > 0
            && regularCount < regularCapacity && examCount < examCapacity;

    public async Task<AnonymousFeedbackSubmission> SubmitAnonymousFeedbackAsync(AnonymousFeedbackRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(request.Rating));

        var themes = (request.Themes ?? Array.Empty<string>())
            .Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var payload = JsonSerializer.Serialize(new { request.Comment, Themes = themes });
        var row = new AnonymousFeedbackSubmission
        {
            Id = Guid.NewGuid(),
            UniversityId = request.UniversityId,
            CourseId = request.CourseId,
            FacultyId = request.FacultyId,
            Rating = request.Rating,
            SentimentScore = request.SentimentScore,
            ThemesCsv = string.Join(',', themes),
            EncryptedPayload = Encrypt(payload)
        };

        db.AnonymousFeedbackSubmissions.Add(row);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Anonymous feedback recorded for course {CourseId} and faculty {FacultyId}", request.CourseId, request.FacultyId);
        return row;
    }

    public async Task<GradeOverrideRequest> RequestGradeOverrideAsync(GradeOverrideRequest request, CancellationToken ct = default)
    {
        if (request.StudentId == Guid.Empty || request.CourseId == Guid.Empty || request.SemesterId == Guid.Empty || request.RequestedBy == Guid.Empty)
            throw new ArgumentException("Student, course, semester, and actor are required.");
        if (request.OriginalMarks is < 0 or > 100 || request.OverrideMarks is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request.OverrideMarks));
        request.Id = request.Id == default ? Guid.NewGuid() : request.Id;
        request.ApprovalStatus = "pending";
        request.ApprovalThreshold = DetermineGradeApprovalThreshold(request.OriginalMarks, request.OverrideMarks);
        db.GradeOverrideRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    private string Encrypt(string plaintext)
    {
        var keyBase64 = configuration["Governance:FeedbackEncryptionKeyBase64"];
        if (string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException("Governance feedback encryption key is not configured.");

        var key = Convert.FromBase64String(keyBase64);
        if (key.Length != 32) throw new InvalidOperationException("Governance feedback encryption key must be 32 bytes for AES-256.");

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }
}
