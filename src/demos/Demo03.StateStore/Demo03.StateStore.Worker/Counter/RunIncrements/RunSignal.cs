using System.Threading.Channels;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

/// <summary>
/// Hands a run from the /run request to <see cref="CounterRunner"/> so the request returns
/// immediately. That is what lets both workers' runs overlap when you fire two curls in a row.
/// </summary>
public sealed class RunSignal
{
    private readonly Channel<RunIncrementsCommand> _runs = Channel.CreateUnbounded<RunIncrementsCommand>();

    public void Trigger() => _runs.Writer.TryWrite(new RunIncrementsCommand());

    public IAsyncEnumerable<RunIncrementsCommand> ReadAllAsync(CancellationToken cancellationToken) =>
        _runs.Reader.ReadAllAsync(cancellationToken);
}
