namespace AegisCore.Application.MicrosoftEcosystem;

public interface ITeamsProvisioningService
{
    Task<TeamsSyncResult> ProvisionClassAsync(Guid timetableEntryId, CancellationToken cancellationToken = default);
}

public interface IMoodleLmsService
{
    Task<LmsSyncResult> SyncRosterAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<LmsSyncResult> SyncGradePassbackAsync(Guid courseId, CancellationToken cancellationToken = default);
}

public interface IPowerBiEmbeddingService
{
    Task<PowerBiEmbedConfig> GetEmbedConfigAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}

public record TeamsSyncResult(Guid LogId, string Status, string? TeamsTeamId, string? ErrorMessage);
public record LmsSyncResult(Guid LogId, string Status, string SyncType, string? ErrorMessage);
public record PowerBiEmbedConfig(string ReportId, string WorkspaceId, string EmbedUrl, string EmbedToken, DateTimeOffset ExpirationTime);

public static class PowerBiRoles
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "vc", "registrar", "dean", "hod", "faculty", "admin", "finance" };
}
