namespace proto_back.Shared.Errors;

public class ServerErrorResponse
{
    public string Error { get; set; } = null!;
    public string ErrorId { get; set; } = null!;
}
