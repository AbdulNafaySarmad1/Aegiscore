namespace AegisCore.Application.Abstractions.Cloud;

public interface ISecretService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
}

public interface IStorageService
{
    Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default);
    Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken cancellationToken = default);
}

public enum StorageVisibility { Private, Public }

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string To,
    string? Cc = null,
    string Subject = "",
    string HtmlBody = "",
    string? PlainTextBody = null,
    string? FromName = null,
    string? FromAddress = null,
    IEnumerable<EmailAttachment>? Attachments = null
);

public record EmailAttachment(string FileName, string ContentType, byte[] Data);

public record CalendarEvent(
    string Title,
    string? Description = null,
    DateTime StartTime = default,
    DateTime EndTime = default,
    string? Location = null,
    string? Timezone = null,
    IEnumerable<string>? AttendeeEmails = null
);

public interface ICalendarService
{
    Task<string> CreateEventAsync(CalendarEvent calendarEvent, string organizerUserId, CancellationToken cancellationToken = default);
    Task UpdateEventAsync(string eventId, CalendarEvent calendarEvent, string organizerUserId, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(string eventId, string organizerUserId, CancellationToken cancellationToken = default);
    Task SendInvitesAsync(string eventId, IEnumerable<string> attendeeEmails, CancellationToken cancellationToken = default);
}

public record ProvisionUserCommand(
    string ExternalId,
    string Email,
    string FirstName,
    string LastName,
    string? Department = null,
    string? Role = null
);

public record ProvisionedUser(
    string ExternalId,
    string Email,
    bool IsProvisioned,
    string? LocalUserId = null
);

public record SsoTokenResult(
    bool IsValid,
    string? UserId = null,
    string? Email = null,
    string? Name = null,
    Dictionary<string, string>? Claims = null,
    string? Error = null
);

public interface IIdentityProviderService
{
    Task<ProvisionedUser> ProvisionUserAsync(ProvisionUserCommand cmd, CancellationToken cancellationToken = default);
    Task DeprovisionUserAsync(string externalId, CancellationToken cancellationToken = default);
    Task<string> GetSsoLoginUrlAsync(string tenantId, string returnUrl, CancellationToken cancellationToken = default);
    Task<SsoTokenResult> ValidateSsoCallbackAsync(string code, string state, CancellationToken cancellationToken = default);
    Task SyncDirectoryAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record PushMessage(
    string Title,
    string Body,
    string? DeepLink = null,
    string? IconUrl = null,
    Dictionary<string, string>? Data = null
);

public interface INotificationPushService
{
    Task SendPushAsync(string deviceToken, PushMessage message, CancellationToken cancellationToken = default);
    Task SendTopicPushAsync(string topic, PushMessage message, CancellationToken cancellationToken = default);
}
