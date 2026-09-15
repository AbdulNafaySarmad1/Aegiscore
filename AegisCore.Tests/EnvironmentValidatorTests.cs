using AegisCore.Infrastructure.Deployment;
using Microsoft.Extensions.Configuration;
using AegisCore.Infrastructure.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace AegisCore.Tests;

public sealed class EnvironmentValidatorTests
{
    [Fact]
    public void Registry_ContainsEverySpecifiedVariable_AndAcceptsLocalConfiguration()
    {
        var values = ValidLocalValues();
        var validator = new EnvironmentValidator(Configuration(values));

        var result = validator.Validate();

        Assert.Equal(27, EnvironmentValidator.Registry.Count);
        Assert.Equal(
            new[]
            {
                "AEGISCORE_ENCRYPTION_KEY", "AEGISCORE_SETUP_TOKEN", "AI_API_KEY", "AI_ENDPOINT", "AI_MODEL", "AI_PROVIDER",
                "APP_ENV", "APP_SECRET", "APP_URL", "CF_TURNSTILE_SECRET_KEY", "CF_TURNSTILE_SITE_KEY", "DB_DIALECT", "DB_HOST", "DB_NAME",
                "DB_PASS", "DB_PORT", "DB_USER", "PQC_MLDSA_PRIVATE_KEY", "PQC_MLDSA_PUBLIC_KEY", "PQC_MLKEM_PRIVATE_KEY",
                "PQC_MLKEM_PUBLIC_KEY", "SESSION_LIFETIME", "SSO_CALLBACK_URL", "SSO_CLIENT_ID", "SSO_CLIENT_SECRET", "SSO_PROVIDER",
                "SSO_TENANT_ID"
            },
            EnvironmentValidator.Registry.Select(definition => definition.Name).Order(StringComparer.Ordinal));
        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Empty(result.MissingRequired);
        Assert.Empty(result.MissingConditional);
    }

    [Fact]
    public void RemoteSsoAndAiProviders_ActivateConditionalRequirements()
    {
        var values = ValidLocalValues();
        values["SSO_PROVIDER"] = "entra";
        values["AI_PROVIDER"] = "openai";
        values.Remove("AI_API_KEY");
        var validator = new EnvironmentValidator(Configuration(values));

        var result = validator.Validate();

        Assert.False(result.IsValid);
        Assert.Equal(
            ["SSO_CALLBACK_URL", "SSO_CLIENT_ID", "SSO_CLIENT_SECRET"],
            validator.GetMissingConditional(EnvironmentValidator.SsoNonLocalCondition));
        Assert.Equal(["SSO_TENANT_ID"], validator.GetMissingConditional(EnvironmentValidator.SsoEntraCondition));
        Assert.Equal(["AI_API_KEY"], validator.GetMissingConditional(EnvironmentValidator.AiRemoteCondition));
    }

    [Fact]
    public void InvalidIntegerAndUrlValues_AreRejected()
    {
        var values = ValidLocalValues();
        values["DB_PORT"] = "not-a-port";
        values["APP_URL"] = "relative/path";
        var result = new EnvironmentValidator(Configuration(values)).Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("DB_PORT", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("APP_URL", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProductionReadiness_MissingRequiredVariable_BlocksStartup()
    {
        var configuration = Configuration(new Dictionary<string, string?>());
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        var checker = new DeploymentReadinessChecker(
            configuration,
            Mock.Of<ILogger<DeploymentReadinessChecker>>(),
            new EnvironmentValidator(configuration),
            environment.Object,
            new PostgresDialectAdapter());

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => checker.RunAllAsync());

        Assert.Contains("STARTUP BLOCKED", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> ValidLocalValues() => new(StringComparer.Ordinal)
    {
        ["DB_HOST"] = "localhost",
        ["DB_PORT"] = "5432",
        ["DB_NAME"] = "aegiscore",
        ["DB_USER"] = "aegiscore",
        ["DB_PASS"] = "secret",
        ["SSO_PROVIDER"] = "local",
        ["AI_PROVIDER"] = "ollama",
        ["AI_ENDPOINT"] = "http://localhost:11434",
        ["AI_MODEL"] = "qwen2.5:7b",
        ["AI_API_KEY"] = "local-test-key",
        ["PQC_MLKEM_PUBLIC_KEY"] = "public",
        ["PQC_MLKEM_PRIVATE_KEY"] = "private",
        ["PQC_MLDSA_PUBLIC_KEY"] = "public",
        ["PQC_MLDSA_PRIVATE_KEY"] = "private",
        ["CF_TURNSTILE_SITE_KEY"] = "site",
        ["CF_TURNSTILE_SECRET_KEY"] = "secret",
        ["APP_URL"] = "https://erp.example.edu",
        ["APP_ENV"] = "Production",
        ["APP_SECRET"] = "secret",
        ["SESSION_LIFETIME"] = "900",
        ["AEGISCORE_SETUP_TOKEN"] = "setup-token",
        ["AEGISCORE_ENCRYPTION_KEY"] = "encryption-key"
    };
}
