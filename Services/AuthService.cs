using System.Collections.Concurrent;
using proto_back.Interfaces.IServices;

namespace proto_back.Services;

public class AuthService : IAuthService
{
    private readonly ConcurrentDictionary<string, bool> _tokens = new();

    public string GenerateAnonymousToken()
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens.TryAdd(token, true);
        return token;
    }

    public bool ValidateToken(string token)
    {
        return _tokens.ContainsKey(token);
    }
}
