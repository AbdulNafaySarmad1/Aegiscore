using System.Net.Mail;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Workflow;

public class EmailNotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly INotificationRecordRepository _notificationRepo;
    private readonly TenantConfigService _tenantConfig;
    private readonly ILogger<EmailNotificationService> _logger;

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

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Recipient}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SMS sending not configured. Message to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }

    public Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In-app notification to {UserId}: {Title} - {Message}", userId, title, message);
        return Task.CompletedTask;
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
            }

            record.Status = NotificationStatus.Sent;
            record.SentAt = DateTime.UtcNow;
            await _notificationRepo.UpdateAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            record.Status = NotificationStatus.Failed;
            record.ErrorMessage = "Delivery failed.";
            await _notificationRepo.UpdateAsync(record, cancellationToken);
        }

        return record;
    }
}
