namespace proto_back.Shared.Errors;

/// <summary>
/// Error body returned for 400 Bad Request responses.
/// </summary>
public class ErrorResponse
{
    /// <summary>Human-readable description of what was wrong with the request.</summary>
    public string Error { get; set; } = null!;
}
