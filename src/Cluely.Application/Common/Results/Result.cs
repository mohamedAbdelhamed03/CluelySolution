namespace Cluely.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Cannot have error with successful result.");
        
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Failure must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result<T> Success<T>(T value) => new(true, null, value);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Failure<T>(Error error) => new(false, error, default!);
}

public class Result<T> : Result
{
    public T Value { get; }

    internal Result(bool isSuccess, Error? error, T value) : base(isSuccess, error)
    {
        Value = value;
    }
}

public abstract record Error(string Code, string Message);
public record ValidationError(string Code, string Message, System.Collections.Generic.IDictionary<string, string[]> Errors) : Error(Code, Message);
public record BusinessError(string Code, string Message) : Error(Code, Message);
public record UnexpectedError(string Code, string Message, Exception? Exception = null) : Error(Code, Message);