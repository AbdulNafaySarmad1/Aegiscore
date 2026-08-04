using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace AegisCore.Infrastructure.Security.Pqc;

public interface IPqcSigningQueue
{
    Task<PqcSignatureResult> ExecuteAsync(Func<PqcSignatureResult> operation, CancellationToken cancellationToken = default);
}

public sealed class PqcSigningQueue : BackgroundService, IPqcSigningQueue
{
    private readonly Channel<SigningWorkItem> _channel = Channel.CreateBounded<SigningWorkItem>(new BoundedChannelOptions(1024)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });

    public async Task<PqcSignatureResult> ExecuteAsync(Func<PqcSignatureResult> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var completion = new TaskCompletionSource<PqcSignatureResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _channel.Writer.WriteAsync(new(operation, completion, cancellationToken), cancellationToken);
        return await completion.Task.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (item.CancellationToken.IsCancellationRequested)
            {
                item.Completion.TrySetCanceled(item.CancellationToken);
                continue;
            }
            try { item.Completion.TrySetResult(item.Operation()); }
            catch (Exception exception) { item.Completion.TrySetException(exception); }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }

    private sealed record SigningWorkItem(
        Func<PqcSignatureResult> Operation,
        TaskCompletionSource<PqcSignatureResult> Completion,
        CancellationToken CancellationToken);
}

internal sealed class InlinePqcSigningQueue : IPqcSigningQueue
{
    public static InlinePqcSigningQueue Instance { get; } = new();
    public Task<PqcSignatureResult> ExecuteAsync(Func<PqcSignatureResult> operation, CancellationToken cancellationToken = default)
        => cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<PqcSignatureResult>(cancellationToken)
            : Task.FromResult(operation());
}
