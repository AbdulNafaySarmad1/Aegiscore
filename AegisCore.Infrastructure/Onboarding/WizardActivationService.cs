namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardActivationService(AegisDbContext db, WizardSessionService sessions, WizardProvisioningService provisioning)
{
    public async Task ActivateAsync(Guid tenantId, Guid sessionId, string confirmedBy, CancellationToken ct)
    {
        var session = await sessions.FindAsync(tenantId, sessionId, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (session.CompletedAt is not null) return;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await sessions.CompleteAsync(tenantId, sessionId, confirmedBy, ct);
            await provisioning.ProvisionAsync(tenantId, session.StepData, ct);
            await transaction.CommitAsync(ct);
        });
    }
}
