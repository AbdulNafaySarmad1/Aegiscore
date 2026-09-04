using System.Collections.Concurrent;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.DataProtection;

namespace AegisCore.Tests;

internal sealed class TestSecretService : ISecretService
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        => Task.FromResult(_values[secretName]);

    public Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        _values[secretName] = value;
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(secretName, out _);
        return Task.CompletedTask;
    }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
        => Task.FromResult(_values.ContainsKey(secretName));
}

internal static class WizardTestState
{
    public static WizardStateService Create(
        AegisDbContext db,
        ISecretService? secrets = null,
        IDataProtectionProvider? protection = null)
        => new(new EfWizardStateStore(
            db,
            secrets ?? new TestSecretService(),
            protection ?? new EphemeralDataProtectionProvider()));
}
