namespace proto_back.Shared.Errors;

/// <summary>
/// An error the client can act on (bad input, unsupported area...), returned with its own
/// status code and a readable message instead of a 500. It is not logged as a crash.
/// </summary>
public abstract class ApiException : Exception
{
    protected ApiException(int statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>HTTP status code returned to the client.</summary>
    public int StatusCode { get; }
}
