using AegisCore.Application.Onboarding;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardActivationService(AegisDbContext db, WizardSessionService sessions, WizardProvisioningService provisioning)
{
    public async Task<WizardStateDto> ActivateAsync(string token, Guid sessionId, string confirmedBy, CancellationToken ct)
    {
        var session = await sessions.FindAsync(token, sessionId, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (session.CompletedAt is not null) throw new WizardSessionCompletedException();
        if (session.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before activation.");

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            session = await sessions.CompleteAsync(token, sessionId, confirmedBy, ct);
            await provisioning.ProvisionAsync(session.TenantId, session.StepData, ct);
            await transaction.CommitAsync(ct);
        });
        return session;
    }
}
