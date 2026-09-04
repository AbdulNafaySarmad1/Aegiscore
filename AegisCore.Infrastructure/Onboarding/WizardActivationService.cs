using AegisCore.Application.Onboarding;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardActivationService
{
    private readonly AegisDbContext _db;
    private readonly WizardSessionService _sessions;
    private readonly WizardProvisioningService _provisioning;
    private readonly EnhancedSetupService? _enhancedSetup;
    private readonly SetupImportService? _imports;

    public WizardActivationService(
        AegisDbContext db,
        WizardSessionService sessions,
        WizardProvisioningService provisioning,
        EnhancedSetupService? enhancedSetup = null,
        SetupImportService? imports = null)
    {
        _db = db;
        _sessions = sessions;
        _provisioning = provisioning;
        _enhancedSetup = enhancedSetup;
        _imports = imports;
    }

    public async Task<WizardStateDto> ActivateAsync(string token, Guid sessionId, string confirmedBy, CancellationToken ct)
    {
        var session = await _sessions.FindAsync(token, sessionId, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (session.CompletedAt is not null) throw new WizardSessionCompletedException();
        if (session.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before activation.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            session = await _sessions.CompleteAsync(token, sessionId, confirmedBy, ct);
            if (_enhancedSetup is not null && _imports is not null)
            {
                var rows = _enhancedSetup.GetValidatedRows();
                if (rows.Count > 0)
                {
                    var import = await _imports.CommitAsync(
                        session.TenantId, session.AdminFullName, session.AdminAccountEmail, session.AdminPassword, rows, ct);
                    if (!import.Success)
                        throw new InvalidOperationException($"Initial data import failed: {string.Join(" ", import.RejectedRows)}");
                }
            }
            await _provisioning.ProvisionAsync(session.TenantId, session.StepData, ct);
            await transaction.CommitAsync(ct);
        });
        return session;
    }
}
