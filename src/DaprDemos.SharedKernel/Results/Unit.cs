namespace DaprDemos.SharedKernel.Results;

/// <summary>The payload for commands that have no return value: such handlers return <c>Result&lt;Unit&gt;</c>.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
