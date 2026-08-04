namespace AegisCore.Application.Finance;

public interface IFinanceRealtimePublisher
{
    Task PublishAsync(Guid tenantId, string eventName, object payload, CancellationToken cancellationToken = default);
}
