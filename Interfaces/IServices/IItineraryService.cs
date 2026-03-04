using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;

namespace proto_back.Interfaces.IServices;

public interface IItineraryService
{
    ItineraryResponse ComputeItinerary(CreateItineraryRequest request);
}
