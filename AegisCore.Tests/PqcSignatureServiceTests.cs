using System.Collections.Concurrent;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Finance;

namespace AegisCore.Tests;

public sealed class PqcSignatureServiceTests
{
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
        Assert.True(service.Verify(result.Algorithm, payload, result.Signature, result.PublicKey));
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
