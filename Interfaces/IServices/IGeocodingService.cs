using proto_back.DTOs.Responses;

namespace proto_back.Interfaces.IServices;

public interface IGeocodingService
{
    Task<PointResponse> ResolvePointAsync(string input);
}
