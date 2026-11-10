namespace ruru_api.common;

/// <summary>
/// The outcome of an operation: either a value (success) or a list of error messages (failure).
/// Used by repositories and services so they can report failures without knowing about HTTP.
/// Controllers turn it into an HTTP response.
/// </summary>
public sealed class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool succeeded, T? value, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, []);
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToList());
    public static Result<T> Failure(string error) => new(false, default, [error]);
}
