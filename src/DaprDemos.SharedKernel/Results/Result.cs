namespace DaprDemos.SharedKernel.Results;

public sealed class Result<TValue>
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("A success result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("A failure result must carry an error.", nameof(error));
        }

        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failure result.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess ? map(Value) : Result<TOut>.Failure(Error);

    public Result<TOut> Bind<TOut>(Func<TValue, Result<TOut>> bind) =>
        IsSuccess ? bind(Value) : Result<TOut>.Failure(Error);

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<Error, Task<TOut>> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}
