namespace proto_back.Shared.Errors;

/// <summary>
/// Error body returned for 500 Internal Server Error responses.
/// </summary>
public class ServerErrorResponse
{
    /// <summary>Human-readable error message. Do not rely on its exact wording.</summary>
    public string Error { get; set; } = null!;

    /// <summary>
    /// Unique identifier for this error. Quote it to the backend team when reporting an
    /// issue: it identifies the corresponding entry in the <c>error_logs</c> MongoDB
    /// collection.
    /// </summary>
    public string ErrorId { get; set; } = null!;
}
