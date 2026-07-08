namespace DaprDemos.SharedKernel.Results;

// Unit result to represent Result that returns no object.
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
