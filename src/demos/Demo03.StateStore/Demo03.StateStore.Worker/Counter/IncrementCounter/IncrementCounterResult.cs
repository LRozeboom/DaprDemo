namespace Demo03.StateStore.Worker.Counter.IncrementCounter;

/// <summary>
/// The new counter value, and how many attempts it took to land it. Every attempt past the first
/// was an ETag conflict: somebody else wrote the key between this caller's read and its write.
/// </summary>
public sealed record IncrementCounterResult(int Value, int Attempts);
