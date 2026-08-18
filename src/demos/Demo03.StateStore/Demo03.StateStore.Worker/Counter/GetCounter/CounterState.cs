namespace Demo03.StateStore.Worker.Counter.GetCounter;

/// <summary>The stored value and the ETag the store currently has for it.</summary>
public sealed record CounterState(int Value, string ETag);
