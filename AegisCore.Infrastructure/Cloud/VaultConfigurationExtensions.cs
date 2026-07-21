using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AegisCore.Infrastructure.Cloud;

public static class VaultConfigurationExtensions
{
    public static void AddVaultSecrets(this IConfigurationBuilder builder, string provider, IConfiguration configuration)
    {
        try
        {
            Dictionary<string, string>? secrets = provider switch
            {
                "AWS" => LoadAws(configuration),
                "GCP" => LoadGcp(configuration),
                "ONPREMISE" or "" or null => LoadLocalEnvFiles(),
                _ => null
            };

            if (secrets is { Count: > 0 })
                builder.AddInMemoryCollection(secrets);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AegisCore] Vault secret loading failed for provider '{provider}': {ex.Message}");
        }
    }

    private static Dictionary<string, string>? LoadAws(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var secretName = configuration["AWS:SecretName"] ?? "aegiscore-config";
        using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }).GetAwaiter().GetResult();
        return ParseSecret(response.SecretString, secretName);
    }

    private static Dictionary<string, string>? LoadGcp(IConfiguration configuration)
    {
        var projectId = configuration["GCP:ProjectId"] ?? throw new InvalidOperationException("GCP:ProjectId not configured.");
        var secretId = configuration["GCP:SecretId"] ?? "aegiscore-config";
        var client = SecretManagerServiceClient.Create();
        var response = client.AccessSecretVersion(new SecretVersionName(projectId, secretId, "latest"));
        return ParseSecret(response.Payload.Data.ToStringUtf8(), secretId);
    }

    private static Dictionary<string, string>? LoadLocalEnvFiles()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in new[] { ".env", "config/.env.generated", "/app/config/.env.generated" })
        {
            if (File.Exists(path))
                foreach (var (key, value) in ParseEnvFile(File.ReadAllText(path)))
                    result[key] = value;
        }
        return result.Count > 0 ? result : null;
    }

    private static Dictionary<string, string> ParseSecret(string? json, string fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(); }
        catch (JsonException) { return new Dictionary<string, string> { [fallbackKey] = json }; }
    }

    private static Dictionary<string, string> ParseEnvFile(string content)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var idx = line.IndexOf('=');
            if (idx <= 0) continue;
            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim().Trim('"').Trim('\'');
            dict[key] = value;
        }
        return dict;
    }
}
