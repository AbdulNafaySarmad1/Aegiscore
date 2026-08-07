using System.Net.Mail;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace AegisCore.Infrastructure.Workflow;

public class EmailNotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly INotificationRecordRepository _notificationRepo;
    private readonly TenantConfigService _tenantConfig;
    private readonly ILogger<EmailNotificationService> _logger;

    private static readonly IAsyncPolicy NotificationPolicy = Policy
        .WrapAsync(
            Policy.Handle<Exception>()
                .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(2)),
            Policy.TimeoutAsync(TimeSpan.FromSeconds(15))
        );

    public EmailNotificationService(
        IConfiguration configuration,
        INotificationRecordRepository notificationRepo,
        TenantConfigService tenantConfig,
        ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _notificationRepo = notificationRepo;
        _tenantConfig = tenantConfig;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? _tenantConfig.GetSmtpHost();
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? _tenantConfig.GetSmtpPort());
        var fromAddress = _configuration["Email:FromAddress"] ?? _tenantConfig.GetFromAddress();
        var fromName = _configuration["Email:FromName"] ?? _tenantConfig.GetFromName();

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) ? ssl : DefaultTenantConfigTemplate.DefaultSslEnabled;

            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new System.Net.NetworkCredential(username, password);
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await NotificationPolicy.ExecuteAsync(
                async innerCt => await client.SendMailAsync(message, innerCt),
                cancellationToken);
            _logger.LogInformation("Email sent to {Recipient}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
    }

    public async Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var twilioSid = _configuration["Twilio:AccountSid"];
        var twilioToken = _configuration["Twilio:AuthToken"];
        var twilioFrom = _configuration["Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(twilioSid) || string.IsNullOrWhiteSpace(twilioToken) || string.IsNullOrWhiteSpace(twilioFrom))
        {
            _logger.LogWarning("SMS not configured — Twilio credentials missing. Message to {Phone}: {Message}", phoneNumber, message);
            return;
        }

        try
        {
            var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{twilioSid}:{twilioToken}"));

            var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{twilioSid}/Messages.json";

            await NotificationPolicy.ExecuteAsync(async innerCt =>
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                using var content = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = phoneNumber,
                    ["From"] = twilioFrom,
                    ["Body"] = message
                });

                var response = await http.PostAsync(requestUrl, content, innerCt);
                response.EnsureSuccessStatusCode();
            }, cancellationToken);
            _logger.LogInformation("SMS sent to {Phone}", phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", phoneNumber);
            throw;
        }
    }

    public async Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new NotificationRecord
            {
                Id = Guid.NewGuid(),
                Recipient = userId.ToString(),
                Subject = title,
                Body = message,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(notification, cancellationToken);
            _logger.LogInformation("In-app notification created for {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create in-app notification for {UserId}", userId);
        }
    }

    public async Task SendWhatsAppAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var whatsappToken = _configuration["WhatsApp:AccessToken"];
        var whatsappPhoneId = _configuration["WhatsApp:PhoneNumberId"];

        if (string.IsNullOrWhiteSpace(whatsappToken) || string.IsNullOrWhiteSpace(whatsappPhoneId))
        {
            _logger.LogWarning("WhatsApp not configured. Message to {Phone}: {Message}", phoneNumber, message);
            return;
        }

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", whatsappToken);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "text",
                text = new { body = message }
            });

            var response = await http.PostAsync(
                $"https://graph.facebook.com/v18.0/{whatsappPhoneId}/messages",
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            _logger.LogInformation("WhatsApp message sent to {Phone}", phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Phone}", phoneNumber);
            throw;
        }
    }

    public async Task<NotificationRecord> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (record.Channel)
            {
                case NotificationChannel.Email:
                    await SendEmailAsync(record.Recipient, record.Subject, record.Body, cancellationToken);
                    break;
                case NotificationChannel.SMS:
                    await SendSmsAsync(record.Recipient, record.Body, cancellationToken);
                    break;
                case NotificationChannel.InApp:
                    break;
                case NotificationChannel.WhatsApp:
                    await SendWhatsAppAsync(record.Recipient, record.Body, cancellationToken);
                    break;
            }

            record.Status = NotificationStatus.Sent;
            record.SentAt = DateTime.UtcNow;
            await _notificationRepo.UpdateAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            record.Status = NotificationStatus.Failed;
            record.ErrorMessage = "Delivery failed.";
            _logger.LogWarning(ex, "Notification delivery failed for {Recipient}", record.Recipient);
            await _notificationRepo.UpdateAsync(record, cancellationToken);
        }

        return record;
    }
}
