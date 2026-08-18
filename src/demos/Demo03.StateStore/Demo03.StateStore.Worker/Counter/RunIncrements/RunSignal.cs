using System.Threading.Channels;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

/// <summary>
/// Queues a run so /run can answer immediately instead of blocking for the whole thing. That is
/// what lets both workers' runs overlap when you fire the two curls one after the other.
/// </summary>
public sealed class RunSignal
{
    private readonly Channel<RunIncrementsCommand> _runs = Channel.CreateUnbounded<RunIncrementsCommand>();

    public void Trigger() => _runs.Writer.TryWrite(new RunIncrementsCommand());

    public IAsyncEnumerable<RunIncrementsCommand> ReadAllAsync(CancellationToken cancellationToken) =>
        _runs.Reader.ReadAllAsync(cancellationToken);
}
