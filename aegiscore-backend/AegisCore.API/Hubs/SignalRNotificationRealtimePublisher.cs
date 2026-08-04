using AegisCore.Application.Platform;
using Microsoft.AspNetCore.SignalR;

namespace AegisCore.API.Hubs;

public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PublishAsync(string recipient, NotificationDeliveryDto delivery, CancellationToken cancellationToken = default)
        => _hub.Clients.Group($"user-{recipient}").SendAsync("ReceiveNotification", delivery, cancellationToken);
}
