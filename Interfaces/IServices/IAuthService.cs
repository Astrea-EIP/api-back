namespace proto_back.Interfaces.IServices;

public interface IAuthService
{
    string GenerateAnonymousToken();
    bool ValidateToken(string token);
}
