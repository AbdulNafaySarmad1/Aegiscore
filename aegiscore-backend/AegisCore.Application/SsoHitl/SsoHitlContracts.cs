using AegisCore.Domain.Entities;

namespace AegisCore.Application.SsoHitl;

public interface IMultiTenantSsoService
{
    string GetEmailDomain(string email);
    string BuildRedirectUrl(UniversitySsoProfile profile, string returnUrl);
    string GenerateTemporaryPassword();
    Task<SsoProviderDetection> DetectProviderAsync(string email, string returnUrl, CancellationToken ct = default);
}

public interface ITwoFactorService
{
    string GenerateSecret();
    string[] GenerateBackupCodes(int count = 10);
    bool VerifyTotp(string base32Secret, string code, DateTimeOffset now);
}

public interface IAgenticExamService
{
    string BuildQrPayload(Guid examId, Guid studentId);
    bool RequiresHumanReview(decimal confidence, decimal threshold = 0.75m);
    PaperDestructionManifest CreateDestructionManifest(Guid batchId, int paperCount, Guid witnessedBy);
}

public sealed record SsoProviderDetection(Guid UniversityId, string UniversityName, string Provider, string RedirectUrl, string? LogoUrl, string? PrimaryColor);
