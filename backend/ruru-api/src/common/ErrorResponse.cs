namespace ruru_api.common;

/// <summary>
/// The body of every error response: <c>{ "message": "...", "errors": ["..."] }</c>.
/// The web app shows <see cref="Message"/>; <see cref="Errors"/> keeps each error separately.
/// </summary>
public sealed class ErrorResponse
{
    public ErrorResponse(IEnumerable<string> errors)
    {
        Errors = errors.ToList();
        Message = string.Join(" ", Errors);
    }

    public ErrorResponse(string error) : this([error])
    {
    }

    public string Message { get; }
    public IReadOnlyList<string> Errors { get; }
}
