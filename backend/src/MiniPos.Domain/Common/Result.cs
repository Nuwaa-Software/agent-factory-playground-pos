namespace MiniPos.Domain.Common;

/// <summary>A failure that is part of the domain (not an exception): maps to an HTTP problem response.</summary>
public sealed record Error(string Code, string Message, ErrorKind Kind = ErrorKind.Validation);

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>Handlers return <c>Result&lt;T&gt;</c> instead of throwing for expected failures.</summary>
public sealed class Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
