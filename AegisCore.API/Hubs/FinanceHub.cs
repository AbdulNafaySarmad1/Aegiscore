using AegisCore.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AegisCore.API.Hubs;

[Authorize]
public sealed class FinanceHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenant = Context.User?.FindFirst("tenant_id")?.Value ?? Context.User?.FindFirst("tid")?.Value;
        if (Guid.TryParse(tenant, out var tenantId) && tenantId != Guid.Empty) await Groups.AddToGroupAsync(Context.ConnectionId, $"finance-{tenantId:D}");
        await base.OnConnectedAsync();
    }
}

public sealed class SignalRFinanceRealtimePublisher(IHubContext<FinanceHub> hub) : IFinanceRealtimePublisher
{
    private static readonly HashSet<string> Events = ["voucher-posted", "payment-settled", "voucher-updated"];

    public Task PublishAsync(Guid tenantId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || !Events.Contains(eventName)) throw new ArgumentException("Finance realtime event is invalid.");
        return hub.Clients.Group($"finance-{tenantId:D}").SendAsync("FinanceEvent", new { eventName, payload }, cancellationToken);
    }
}
