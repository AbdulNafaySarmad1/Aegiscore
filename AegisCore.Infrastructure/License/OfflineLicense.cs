using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AegisCore.Application.Licensing;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Licensing;

/// <summary>
/// An offline licence key: the licence payload signed by the vendor's RSA issuing key, verified
/// here with the vendor's public key and nothing else — no network, no shared secret in the
/// container. It is time-boxed by its expiry date and bound to one university id.
/// <para>
/// Format: <c>AEGIS-OFFLINE.&lt;base64url payload&gt;.&lt;base64url RSA-SHA256 signature&gt;</c>. The payload is the
/// same JSON the online licence carries (university_id, university_name, issued_date, expiry_date,
/// max_concurrent_users, issued_by, tier) plus <c>mode: "offline"</c>.
/// </para>
/// <para>
/// The vendor keeps the private key; the public key is compiled in as the default and can be
/// replaced with <c>License:IssuerPublicKey</c> (PEM) or <c>License:IssuerPublicKeyPath</c>. The
/// vendor issues keys with <c>dotnet AegisCore.API.dll issue-license</c>.
/// </para>
/// </summary>
public static class OfflineLicense
{
    public const string Prefix = "AEGIS-OFFLINE.";
    public const string IssuerPublicKeyKey = "License:IssuerPublicKey";
    public const string IssuerPublicKeyPathKey = "License:IssuerPublicKeyPath";

    /// <summary>The vendor's licence-issuing public key. The matching private key never ships.</summary>
    public const string DefaultIssuerPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAp1VEx6gtKDPX0qTLGszQ
        oKuvb8JQIuxG1+gyVpL5qpLZG4yi4BhrKW6uSJSBetr6WA4opFdfBkZTptAoH/cu
        zcwokgBxapFnIdYWo4Zwfe5BbXyx3+sjiekOfbLMg7FvoCXuMUUtVwanla4JLrhJ
        gbvJLBcZqTWSwJcNOXJiZT+/T0Yx9wvuyLcDGd7DKA+i+Hcm+2WtoC+NZ2qY59sc
        iKxYJR+/ho3tpXhwHOxo/QoEbCJ/3KvDl840CgpD+RrzhDq8UFSkLX81qNGIk2hj
        uS3VbMsNMJPXsMn/jaX1rSOXTQqLbI0HkMvZY/N3ufXWPVbKaP0r5gd4XTlKVk+R
        M7lXn+q1q64mApsQk2AoCaRgUs3icx5h3hBP9eVFZr1uJCfThF2BgLrcCVLLoSZM
        UIsC0PKI9QBZn19uwQ+7lFYy1RPAbdTW2wbcMvZAoA5ikv3+XkdalLqWY0uL905c
        dv+qsKQhyaBur7Yij1bp5qwup5z4Mz+fOfBHq9+65UG5AgMBAAE=
        -----END PUBLIC KEY-----
        """;

    public static bool IsOfflineKey(string? licenseKey) => licenseKey is not null && licenseKey.StartsWith(Prefix, StringComparison.Ordinal);

    public static string IssuerPublicKeyPem(IConfiguration configuration)
    {
        var inline = configuration[IssuerPublicKeyKey];
        if (!string.IsNullOrWhiteSpace(inline)) return inline;
        var path = configuration[IssuerPublicKeyPathKey];
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return File.ReadAllText(path);
        return DefaultIssuerPublicKeyPem;
    }

    /// <summary>Vendor side: signs a licence payload with the issuing private key.</summary>
    public static string Issue(string issuerPrivateKeyPem, LicenseInfo license, string tier = "Basic")
    {
        var payload = new JsonObject
        {
            ["mode"] = "offline",
            ["university_id"] = license.UniversityId.ToString(),
            ["university_name"] = license.UniversityName,
            ["issued_date"] = license.IssuedDate.ToString("yyyy-MM-dd"),
            ["expiry_date"] = license.ExpiryDate.ToString("yyyy-MM-dd"),
            ["max_concurrent_users"] = license.MaxConcurrentUsers,
            ["issued_by"] = license.IssuedBy,
            ["tier"] = tier,
        };
        var payloadBytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        using var rsa = RSA.Create();
        rsa.ImportFromPem(issuerPrivateKeyPem);
        var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Prefix + Base64Url(payloadBytes) + "." + Base64Url(signature);
    }

    /// <summary>Client side: verifies the signature with the issuer's public key and reads the payload.</summary>
    public static LicenseValidationResult Validate(string licenseKey, string issuerPublicKeyPem, DateOnly today)
    {
        if (!IsOfflineKey(licenseKey)) return new(false, "Not an offline licence key.", null, LicenseValidationFailureReason.InvalidSignature);
        var parts = licenseKey[Prefix.Length..].Split('.');
        if (parts.Length != 2) return new(false, "Offline licence key is malformed: expected a payload and a signature.", null, LicenseValidationFailureReason.InvalidSignature);
        byte[] payloadBytes, signature;
        try { payloadBytes = FromBase64Url(parts[0]); signature = FromBase64Url(parts[1]); }
        catch (FormatException) { return new(false, "Offline licence key is malformed: it is not the key the vendor issued, or it was altered when copied.", null, LicenseValidationFailureReason.InvalidSignature); }

        using var rsa = RSA.Create();
        try { rsa.ImportFromPem(issuerPublicKeyPem); }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        { return new(false, "The licence issuer public key configured on this installation is not a valid PEM public key (License:IssuerPublicKey).", null, LicenseValidationFailureReason.InvalidSignature); }
        if (!rsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            return new(false, "Offline licence signature is invalid: this key was not issued for this product by the vendor, or it was altered.", null, LicenseValidationFailureReason.InvalidSignature);

        JsonObject payload;
        try { payload = JsonNode.Parse(payloadBytes)?.AsObject() ?? throw new JsonException(); }
        catch (JsonException) { return new(false, "Offline licence payload is not valid JSON.", null, LicenseValidationFailureReason.InvalidSignature); }
        try
        {
            var info = new LicenseInfo(
                Guid.Parse(Field(payload, "university_id")),
                Field(payload, "university_name"),
                DateOnly.Parse(Field(payload, "issued_date")),
                DateOnly.Parse(Field(payload, "expiry_date")),
                int.Parse(Field(payload, "max_concurrent_users")),
                Field(payload, "issued_by"));
            if (info.IssuedDate > today) return new(false, $"Offline licence is not valid before {info.IssuedDate:d MMMM yyyy}.", info, LicenseValidationFailureReason.InvalidSignature);
            if (info.ExpiryDate < today) return new(false, $"Offline licence expired on {info.ExpiryDate:d MMMM yyyy}. Ask the vendor for a renewed key.", info, LicenseValidationFailureReason.Expired);
            return new(true, $"Offline licence for {info.UniversityName} is valid until {info.ExpiryDate:d MMMM yyyy}.", info, null);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return new(false, "Offline licence payload is malformed: " + ex.Message, null, LicenseValidationFailureReason.InvalidSignature);
        }
    }

    public static string Tier(string licenseKey)
    {
        try
        {
            var payload = JsonNode.Parse(FromBase64Url(licenseKey[Prefix.Length..].Split('.')[0]))?.AsObject();
            return payload?["tier"]?.ToString() ?? "Basic";
        }
        catch { return "Basic"; }
    }

    /// <summary>The payload without the signature, for display and storage.</summary>
    public static string PayloadJson(string licenseKey)
        => Encoding.UTF8.GetString(FromBase64Url(licenseKey[Prefix.Length..].Split('.')[0]));

    private static string Field(JsonObject payload, string key)
        => payload[key]?.ToString() ?? throw new InvalidOperationException($"field '{key}' is missing");

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
