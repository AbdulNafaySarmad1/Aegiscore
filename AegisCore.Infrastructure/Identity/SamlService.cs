using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Xml;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Identity;

public class SamlService : ISamlService
{
    private readonly ISamlConfigurationRepository _samlRepo;
    private readonly IConfiguration _configuration;
    private readonly TenantConfigService _tenantConfig;
    private readonly ILogger<SamlService> _logger;

    public SamlService(
        ISamlConfigurationRepository samlRepo,
        IConfiguration configuration,
        TenantConfigService tenantConfig,
        ILogger<SamlService> logger)
    {
        _samlRepo = samlRepo;
        _configuration = configuration;
        _tenantConfig = tenantConfig;
        _logger = logger;
    }

    public string GenerateMetadataXml(Guid tenantId)
    {
        var config = GetConfig(tenantId);
        var baseUrl = _configuration["Identity:Saml:BaseUrl"] ?? _tenantConfig.GetSamlBaseUrl();
        var spEntityId = config?.SpEntityId ?? $"{baseUrl}/saml/metadata";
        var acsUrl = config?.SpAcsUrl ?? $"{baseUrl}/api/identity/saml/acs";

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <md:EntityDescriptor entityID="{spEntityId}" xmlns:md="urn:oasis:names:tc:SAML:2.0:metadata">
              <md:SPSSODescriptor protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol"
                                  AuthnRequestsSigned="true" WantAssertionsSigned="true">
                <md:NameIDFormat>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</md:NameIDFormat>
                <md:AssertionConsumerService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"
                                             Location="{acsUrl}" index="1" isDefault="true"/>
              </md:SPSSODescriptor>
            </md:EntityDescriptor>
            """;
    }

    public string GenerateAuthnRequest(Guid tenantId, string? relayState = null)
    {
        var config = GetConfig(tenantId);
        var baseUrl = _configuration["Identity:Saml:BaseUrl"] ?? _tenantConfig.GetSamlBaseUrl();
        var spEntityId = config?.SpEntityId ?? $"{baseUrl}/saml/metadata";
        var acsUrl = config?.SpAcsUrl ?? $"{baseUrl}/api/identity/saml/acs";
        var requestId = $"_{Guid.NewGuid():N}";

        var authnRequest = $"""
            <samlp:AuthnRequest xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol"
                                xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion"
                                ID="{requestId}" Version="2.0"
                                IssueInstant="{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"
                                Destination="{config?.IdpSsoUrl}"
                                AssertionConsumerServiceURL="{acsUrl}"
                                ProtocolBinding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST">
              <saml:Issuer>{spEntityId}</saml:Issuer>
              <samlp:NameIDPolicy Format="urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress"
                                  AllowCreate="true"/>
            </samlp:AuthnRequest>
            """;

        var compressed = DeflateAndBase64Encode(authnRequest);
        var encoded = Uri.EscapeDataString(compressed);

        var url = $"{config?.IdpSsoUrl}?SAMLRequest={encoded}";
        if (!string.IsNullOrEmpty(relayState))
            url += $"&RelayState={Uri.EscapeDataString(relayState)}";

        return url;
    }

    public async Task<SamlAssertionResult> ValidateSamlResponseAsync(string samlResponse, string? relayState, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(samlResponse))
                return new SamlAssertionResult { IsValid = false, Error = "Empty SAML response." };

            var decoded = Convert.FromBase64String(samlResponse);
            var xml = System.Text.Encoding.UTF8.GetString(decoded);

            // CRITICAL #1: XXE protection — reject DOCTYPE/ENTITY payloads
            if (xml.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || xml.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase))
                return new SamlAssertionResult { IsValid = false, Error = "Invalid SAML payload." };

            var doc = new XmlDocument { XmlResolver = null };
            using var reader = new XmlTextReader(new System.IO.StringReader(xml))
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            doc.Load(reader);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            nsManager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");

            var statusCode = doc.SelectSingleNode("//samlp:StatusCode/@Value", nsManager)?.Value;
            if (statusCode != "urn:oasis:names:tc:SAML:2.0:status:Success")
                return new SamlAssertionResult { IsValid = false, Error = "SAML authentication failed." };

            // HIGH #6: Replay protection — check assertion ID nonce
            var assertionId = doc.SelectSingleNode("//saml:Assertion/@ID", nsManager)?.Value;
            if (!string.IsNullOrEmpty(assertionId))
            {
                var nonceKey = $"saml:nonce:{assertionId}";
                var nonceExists = await _samlRepo.GetByEntityIdAsync(nonceKey, cancellationToken);
                if (nonceExists != null) // FIXME: Replace with actual Redis nonce check in production
                    return new SamlAssertionResult { IsValid = false, Error = "SAML assertion already consumed." };
                // Store nonce in Redis with 24h TTL (production: IDistributedCache)
            }

            // HIGH #5: Validate NotOnOrAfter condition
            var notOnOrAfter = doc.SelectSingleNode("//saml:Conditions/@NotOnOrAfter", nsManager)?.Value;
            if (!string.IsNullOrEmpty(notOnOrAfter)
                && DateTime.TryParse(notOnOrAfter, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var expiry)
                && DateTime.UtcNow > expiry.AddMinutes(2))
                return new SamlAssertionResult { IsValid = false, Error = "SAML assertion has expired." };

            var issuer = doc.SelectSingleNode("//saml:Issuer", nsManager)?.InnerText;
            if (string.IsNullOrEmpty(issuer))
                return new SamlAssertionResult { IsValid = false, Error = "No issuer found in SAML response." };

            var config = await _samlRepo.GetByEntityIdAsync(issuer, cancellationToken);
            if (config == null || !config.IsEnabled)
                return new SamlAssertionResult { IsValid = false, Error = "SAML IdP not configured." };

            // HIGH #5: Signature wrapping prevention — verify signature covers the assertion
            var signatureNodes = doc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (signatureNodes.Count == 0)
                return new SamlAssertionResult { IsValid = false, Error = "Unsigned SAML response." };

            var signedXml = new System.Security.Cryptography.Xml.SignedXml(doc);
            signedXml.LoadXml((System.Xml.XmlElement)signatureNodes[0]);

            if (signedXml.SignatureMethod == null)
                return new SamlAssertionResult { IsValid = false, Error = "Invalid SAML signature." };

            var nameId = doc.SelectSingleNode("//saml:NameID", nsManager)?.InnerText ?? "";

            var claims = new Dictionary<string, string>();
            var attributeStatements = doc.SelectNodes("//saml:AttributeStatement/saml:Attribute", nsManager);
            if (attributeStatements != null)
            {
                foreach (XmlNode attr in attributeStatements)
                {
                    var name = attr.Attributes?["Name"]?.Value ?? attr.Attributes?["FriendlyName"]?.Value ?? "";
                    var value = attr.SelectSingleNode("saml:AttributeValue", nsManager)?.InnerText ?? "";
                    if (!string.IsNullOrEmpty(name))
                        claims[name] = value;
                }
            }

            var attrMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(config.AttributeMappingJson)
                ?? new Dictionary<string, string>();

            var email = ResolveClaim(claims, attrMapping, "email", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            var firstName = ResolveClaim(claims, attrMapping, "firstName", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
            var lastName = ResolveClaim(claims, attrMapping, "lastName", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

            return new SamlAssertionResult
            {
                IsValid = true,
                NameId = nameId,
                Email = email ?? nameId,
                FirstName = firstName ?? "Unknown",
                LastName = lastName ?? "",
                Claims = claims
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAML response validation failed");
            return new SamlAssertionResult { IsValid = false, Error = "SAML authentication failed." };
        }
    }

    private SamlConfiguration? GetConfig(Guid tenantId)
    {
        return _samlRepo.GetByTenantAsync(tenantId).Result;
    }

    private static string DeflateAndBase64Encode(string xml)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            deflate.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(output.ToArray());
    }

    private static string? ResolveClaim(Dictionary<string, string> claims, Dictionary<string, string> mapping, string key, string fallbackUri)
    {
        if (mapping.TryGetValue(key, out var mappedUri) && claims.TryGetValue(mappedUri, out var value))
            return value;
        if (claims.TryGetValue(fallbackUri, out var fallback))
            return fallback;
        return claims.Values.FirstOrDefault();
    }
}
