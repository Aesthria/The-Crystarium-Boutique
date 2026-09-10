namespace CrystariumBoutique.Core.Errors;

public sealed record Result
{
    private Result(bool isSuccess, BoutiqueError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public BoutiqueError? Error { get; }

    public static Result Ok { get; } = new(true, null);

    public static Result Failure(BoutiqueError error)
        => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Success<T>(T value)
        where T : notnull
        => new(true, value ?? throw new ArgumentNullException(nameof(value)), null);

    public static Result<T> Failure<T>(BoutiqueError error)
        where T : notnull
        => new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}

public sealed record Result<T>
    where T : notnull
{
    internal Result(bool isSuccess, T? value, BoutiqueError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public BoutiqueError? Error { get; }

}
