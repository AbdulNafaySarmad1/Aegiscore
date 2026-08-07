using AegisCore.Application.ApiGateway;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/developer/keys")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApiKeyTier>(request.Tier, true, out var tier))
            return BadRequest(new { error = "invalid_tier", message = $"Unknown tier: {request.Tier}. Valid: Free, Basic, Pro, Enterprise." });

        var apiKey = await _apiKeyService.CreateApiKeyAsync(request.Name, tier, request.OwnerName, request.OwnerEmail, cancellationToken);

        return Ok(new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            ApiKey = apiKey.KeyHash,
            Tier = apiKey.Tier.ToString(),
            Status = apiKey.Status.ToString(),
            Permission = apiKey.Permission.ToString(),
            RateLimitPerMinute = apiKey.RateLimitPerMinute,
            RateLimitPerDay = apiKey.RateLimitPerDay,
            CreatedAt = apiKey.CreatedAt,
            OwnerName = apiKey.OwnerName,
            OwnerEmail = apiKey.OwnerEmail
        });
    }

    [HttpPost("{id:guid}/rotate")]
    public async Task<IActionResult> RotateKey(Guid id, CancellationToken cancellationToken)
    {
        var rotated = await _apiKeyService.RotateApiKeyAsync(id, cancellationToken);

        return Ok(new
        {
            rotated.Id,
            rotated.Name,
            rotated.KeyPrefix,
            NewApiKey = rotated.KeyHash,
            Message = "API key rotated successfully. Old key has been invalidated."
        });
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> RevokeKey(Guid id, [FromBody] RevokeApiKeyRequest request, CancellationToken cancellationToken)
    {
        await _apiKeyService.RevokeApiKeyAsync(id, request.Reason, cancellationToken);

        return Ok(new { message = "API key revoked.", reason = request.Reason });
    }
}

[ApiController]
[Route("api/developer/webhooks")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookSubscriptionRepository _webhookRepo;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookSubscriptionRepository webhookRepo, ILogger<WebhooksController> logger)
    {
        _webhookRepo = webhookRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var subscriptions = await _webhookRepo.GetActiveAsync(cancellationToken);
        var response = subscriptions.Select(s => new WebhookSubscriptionResponse
        {
            Id = s.Id,
            Name = s.Name,
            EndpointUrl = s.EndpointUrl,
            EventType = s.EventType,
            Status = s.Status.ToString(),
            RetryCount = s.RetryCount,
            LastDeliveredAt = s.LastDeliveredAt,
            CreatedAt = s.CreatedAt
        });

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] WebhookSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = new Domain.Entities.WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            EndpointUrl = request.EndpointUrl,
            EventType = request.EventType,
            Secret = request.Secret,
            Status = WebhookStatus.Active
        };

        await _webhookRepo.AddAsync(subscription, cancellationToken);

        return Ok(new WebhookSubscriptionResponse
        {
            Id = subscription.Id,
            Name = subscription.Name,
            EndpointUrl = subscription.EndpointUrl,
            EventType = subscription.EventType,
            Status = subscription.Status.ToString(),
            CreatedAt = subscription.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await _webhookRepo.GetByIdAsync(id, cancellationToken);
        if (subscription == null)
            return NotFound(new { error = "not_found", message = "Webhook subscription not found." });

        subscription.Status = WebhookStatus.Disabled;
        await _webhookRepo.UpdateAsync(subscription, cancellationToken);

        return Ok(new { message = "Webhook subscription disabled." });
    }
}

[ApiController]
[Route("api/developer/info")]
public class DeveloperInfoController : ControllerBase
{
    [HttpGet("events")]
    public IActionResult GetEventCatalog()
    {
        var events = new[]
        {
            new { type = "student.enrolled", description = "A new student has been enrolled." },
            new { type = "payment.received", description = "A fee payment has been received." },
            new { type = "result.published", description = "Exam results have been published." },
            new { type = "attendance.marked", description = "Attendance has been recorded for a session." },
            new { type = "leave.requested", description = "An employee has submitted a leave request." },
            new { type = "leave.approved", description = "A leave request has been approved." },
            new { type = "maintenance.ticket_created", description = "A new maintenance ticket has been created." },
            new { type = "security.incident_reported", description = "A security incident has been reported." }
        };

        return Ok(new { events, total = events.Length });
    }

    [HttpGet("tiers")]
    public IActionResult GetTierInfo()
    {
        var tiers = new[]
        {
            new { tier = "Free", requestsPerMinute = 30, requestsPerDay = 5000, features = new[] { "Read-only access", "1 API key", "Community support" } },
            new { tier = "Basic", requestsPerMinute = 120, requestsPerDay = 20000, features = new[] { "Read-only access", "5 API keys", "Email support" } },
            new { tier = "Pro", requestsPerMinute = 600, requestsPerDay = 100000, features = new[] { "Read-write access", "25 API keys", "Priority support", "Webhook access" } },
            new { tier = "Enterprise", requestsPerMinute = 3000, requestsPerDay = 500000, features = new[] { "Full access", "Unlimited API keys", "Dedicated support", "Custom SLAs", "SSO integration" } }
        };

        return Ok(new { tiers, current = "Free" });
    }
}
