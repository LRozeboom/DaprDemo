namespace Demo03.StateStore.Worker.Counter.RunIncrements;

public sealed record RunSummary(int Iterations, int Succeeded, int Failed, int LastObserved);
