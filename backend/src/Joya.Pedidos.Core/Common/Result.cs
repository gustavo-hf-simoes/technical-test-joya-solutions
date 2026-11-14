namespace Joya.Pedidos.Core.Common;

/// <summary>Resultado de um caso de uso: sucesso com valor ou falha com <see cref="Error"/>. Nunca lança por regra de negócio.</summary>
public sealed class Result<T>
    where T : notnull
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
    }

    private Result(Error error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => _error is not null;

    /// <summary>O valor do sucesso. Lança <see cref="InvalidOperationException"/> se for falha (erro de programação).</summary>
    public T Value => IsSuccess && _value is not null
        ? _value
        : throw new InvalidOperationException("Um resultado de falha não tem valor.");

    /// <summary>O erro da falha. Lança <see cref="InvalidOperationException"/> se for sucesso (erro de programação).</summary>
    public Error Error => _error ?? throw new InvalidOperationException("Um resultado de sucesso não tem erro.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResultado Match<TResultado>(Func<T, TResultado> aoSucesso, Func<Error, TResultado> aoFalhar) =>
        _error is null ? aoSucesso(Value) : aoFalhar(_error);
}
