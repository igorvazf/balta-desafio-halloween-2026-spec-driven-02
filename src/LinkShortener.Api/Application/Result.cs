using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Api.Application;

public enum LinkErrorType
{
    Validation,
    Conflict,
    NotFound,
    Expired,
    CodeGenerationFailed,
}

/// <param name="Field">Campo da requisição que causou o erro, quando houver.</param>
public sealed record LinkError(LinkErrorType Type, string Message, string? Field = null);

public sealed class Result<T>
{
    private Result(T? value, LinkError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public LinkError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(LinkError error) => new(default, error);
}
