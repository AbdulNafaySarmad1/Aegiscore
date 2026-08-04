using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Finance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class PqcSignatureServiceTests
{
    [Fact]
    public void KeyHealth_Only_Flags_Keys_After_Inactivity_Window()
    {
        var now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.False(PqcKeyHealth.IsDead(now.AddDays(-1), null, now, 30));
        Assert.True(PqcKeyHealth.IsDead(now.AddDays(-31), null, now, 30));
        Assert.False(PqcKeyHealth.IsDead(now.AddDays(-90), now.AddDays(-2), now, 30));
        Assert.True(PqcKeyHealth.IsDead(now.AddDays(-90), now.AddDays(-31), now, 30));
    }

    [Fact]
    public async Task SigningQueue_Executes_Work_And_Propagates_Failures()
    {
        var queue = new PqcSigningQueue();
        await queue.StartAsync(CancellationToken.None);
        try
        {
            var result = await queue.ExecuteAsync(() => new PqcSignatureResult("TEST", "key", [1], [2]));
            Assert.Equal("key", result.KeyId);
            await Assert.ThrowsAsync<CryptographicException>(() => queue.ExecuteAsync(() => throw new CryptographicException("failed")));
        }
        finally { await queue.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public void MerkleTreeDuplicatesOddLeafWithoutMutatingInput()
    {
        var first = Encoding.UTF8.GetBytes("first");
        var root = FinanceMerkleTree.ComputeRoot([first, Encoding.UTF8.GetBytes("second"), Encoding.UTF8.GetBytes("third")]);

        Assert.Equal(32, root.Length);
        Assert.Equal("first", Encoding.UTF8.GetString(first));
    }

    [Fact]
    public void MonthlySchedulePreservesTotalWithFinalRoundingAdjustment()
    {
        var plan = new AegisCore.Domain.Entities.FinancePaymentPlan
        {
            TenantId = Guid.NewGuid(), FeeDemandId = Guid.NewGuid(), PlanType = "monthly",
            TotalAmount = 100m, StartDate = new DateTime(2026, 8, 1)
        };

        var installments = FinancePaymentPlanSchedule.Build(plan, 3);

        Assert.Equal(plan.TotalAmount, installments.Sum(x => x.Amount));
        Assert.Equal(new DateTime(2026, 9, 1), installments[1].DueDate);
        Assert.Equal(3, installments.Count);
    }

    [Fact]
    public async Task DilithiumSignatureRoundTripsAndRejectsTampering()
    {
        var secrets = new MemorySecretService();
        var service = new PqcSignatureService(secrets);
        var tenantId = Guid.NewGuid();
        var payload = Encoding.UTF8.GetBytes("voucher:immutable");

        var result = await service.SignAsync(tenantId, "voucher", payload, archival: false);

        Assert.Equal("ML-DSA-65", result.Algorithm);
        Assert.Equal(1952, result.PublicKey.Length);
        Assert.Equal(3309, result.Signature.Length);
        Assert.True(service.Verify(result.Algorithm, payload, result.Signature, result.PublicKey));
        Assert.False(service.Verify(result.Algorithm, Encoding.UTF8.GetBytes("voucher:changed"), result.Signature, result.PublicKey));

        var reloaded = await new PqcSignatureService(secrets).SignAsync(tenantId, "voucher", payload, archival: false);
        Assert.True(service.Verify(reloaded.Algorithm, payload, reloaded.Signature, reloaded.PublicKey));
    }

    [Fact]
    public async Task SphincsSignatureRoundTrips()
    {
        var service = new PqcSignatureService(new MemorySecretService());
        var payload = Encoding.UTF8.GetBytes("archive:voucher");

        var result = await service.SignAsync(Guid.NewGuid(), "archive", payload, archival: true);

        Assert.Equal("SLH-DSA-SHA2-128s", result.Algorithm);
        Assert.Equal(32, result.PublicKey.Length);
        Assert.Equal(7856, result.Signature.Length);
        Assert.True(service.Verify(result.Algorithm, payload, result.Signature, result.PublicKey));
    }

    [Fact]
    public async Task HybridKemDerivesSameSecretAndBindsContext()
    {
        var service = new HybridKemService(new MemorySecretService());
        var tenantId = Guid.NewGuid();
        var context = Encoding.UTF8.GetBytes("tenant:aegiscore|channel:finance");
        var publicKey = await service.GetOrCreatePublicKeyAsync(tenantId);

        var encapsulation = service.Encapsulate(publicKey, context);
        var recovered = await service.DecapsulateAsync(tenantId, encapsulation.Envelope, context);
        var wrongContext = await service.DecapsulateAsync(tenantId, encapsulation.Envelope, Encoding.UTF8.GetBytes("tenant:other"));

        Assert.Equal(encapsulation.SharedSecret, recovered);
        Assert.NotEqual(encapsulation.SharedSecret, wrongContext);
        Assert.Equal(1568, publicKey.MLKemPublicKey.Length);
        Assert.Equal(1568, encapsulation.Envelope.MLKemCiphertext.Length);
        Assert.Equal(32, encapsulation.SharedSecret.Length);
        Assert.Equal("ML-KEM-1024+X25519", encapsulation.Envelope.Algorithm);
    }

    [Fact]
    public void AesGcmUsesUniqueNonceAndAuthenticatesAssociatedData()
    {
        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
        var service = new PqcCryptographyService(
            Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = key }),
            NullLogger<PqcCryptographyService>.Instance);
        var payload = Encoding.UTF8.GetBytes("credential");
        var aad = Encoding.UTF8.GetBytes("tenant:aegiscore");

        var first = service.Encrypt(payload, aad);
        var second = service.Encrypt(payload, aad);

        Assert.NotEqual(first[..12], second[..12]);
        Assert.Equal(payload, service.Decrypt(first, aad));
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => service.Decrypt(first, Encoding.UTF8.GetBytes("tenant:other")));
    }

    private sealed class MemorySecretService : ISecretService
    {
        private readonly ConcurrentDictionary<string, string> values = new(StringComparer.Ordinal);

        public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
            => Task.FromResult(values[secretName]);

        public Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
        {
            values[secretName] = value;
            return Task.CompletedTask;
        }

        public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            values.TryRemove(secretName, out _);
            return Task.CompletedTask;
        }

        public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
            => Task.FromResult(values.ContainsKey(secretName));
    }
}
