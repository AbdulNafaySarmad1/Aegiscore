using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AegisCore.API.Hubs;

[Authorize]
public class StateMachineHub : Hub
{
    public async Task Subscribe(string machineKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, machineKey);
        await Clients.Caller.SendAsync("Subscribed", machineKey);
    }

    public async Task Unsubscribe(string machineKey)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, machineKey);
        await Clients.Caller.SendAsync("Unsubscribed", machineKey);
    }

    public async Task NotifyTransition(string machineKey, string state)
    {
        await Clients.Group(machineKey).SendAsync("Transition", machineKey, state);
    }
}
