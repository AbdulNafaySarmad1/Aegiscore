namespace AegisCore.Application.ApiGateway;

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = "Free";
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string? AllowedDomains { get; set; }
}

public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; }
    public int RateLimitPerDay { get; set; }
    public long TotalRequests { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
}

public class RevokeApiKeyRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class WebhookSubscriptionRequest
{
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Secret { get; set; }
}

public class WebhookSubscriptionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime? LastDeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RateLimitTierConfig
{
    public string Tier { get; set; } = "Free";
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerDay { get; set; } = 10000;
    public int BurstLimit { get; set; } = 10;
}
