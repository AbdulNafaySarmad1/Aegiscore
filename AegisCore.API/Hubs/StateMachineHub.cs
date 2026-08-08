using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AegisCore.API.Hubs;

[Authorize]
public class StateMachineHub : Hub
{
    public async Task Subscribe(string machineKey)
    {
        var group = GetTenantGroup(machineKey);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("Subscribed", machineKey);
    }

    public async Task Unsubscribe(string machineKey)
    {
        var group = GetTenantGroup(machineKey);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("Unsubscribed", machineKey);
    }

    private string GetTenantGroup(string machineKey)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || machineKey.Length > 200)
            throw new HubException("Machine key is invalid.");

        var tenant = Context.User?.FindFirst("tenant_id")?.Value ?? Context.User?.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tenant, out var tenantId) || tenantId == Guid.Empty)
            throw new HubException("Tenant context is required.");

        return $"statemachine-{tenantId:D}:{machineKey}";
    }
}
