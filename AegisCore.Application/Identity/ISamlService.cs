namespace AegisCore.Application.Identity;

public interface ISamlService
{
    string GenerateMetadataXml(Guid tenantId);
    string GenerateAuthnRequest(Guid tenantId, string? relayState = null);
    Task<SamlAssertionResult> ValidateSamlResponseAsync(string samlResponse, string? relayState, CancellationToken cancellationToken = default);
}

public class SamlAssertionResult
{
    public bool IsValid { get; set; }
    public string? NameId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
    public string? Error { get; set; }
}
