namespace proto_back.Shared.Errors;

/// <summary>
/// Error body returned for client errors (400 and 422), with a readable message.
/// </summary>
public class ErrorResponse
{
    /// <summary>Human-readable description of what was wrong with the request.</summary>
    public string Error { get; set; } = null!;
}
