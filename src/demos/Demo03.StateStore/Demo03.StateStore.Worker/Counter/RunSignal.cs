using System.Threading.Channels;

namespace Demo03.StateStore.Worker.Counter;

/// <summary>Lets the /run endpoint arm the background increment loop.</summary>
public sealed class RunSignal
{
    private readonly Channel<bool> _runs = Channel.CreateUnbounded<bool>();

    public void Trigger() => _runs.Writer.TryWrite(true);

    public IAsyncEnumerable<bool> WaitForRunsAsync(CancellationToken cancellationToken) =>
        _runs.Reader.ReadAllAsync(cancellationToken);
}
