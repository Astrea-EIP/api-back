using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Services;

/// <summary>
/// Mock itinerary service returning a hardcoded Lille → Nantes route.
/// Will be replaced by the real itinerary engine integration later.
/// </summary>
public class ItineraryService : IItineraryService
{
    // Hardcoded polyline: Lille (50.6292, 3.0573) → Nantes (47.2184, -1.5536)
    // Intermediate waypoints approximate a road route via Paris region
    private static readonly List<PointResponse> LilleToNantesRoute = new()
    {
        new PointResponse { Lat = 50.6292, Lng = 3.0573 },   // Lille
        new PointResponse { Lat = 50.2910, Lng = 2.7775 },   // Arras
        new PointResponse { Lat = 49.8941, Lng = 2.2958 },   // Amiens
        new PointResponse { Lat = 49.4432, Lng = 2.0731 },   // Beauvais
        new PointResponse { Lat = 48.8566, Lng = 2.3522 },   // Paris
        new PointResponse { Lat = 48.4469, Lng = 1.4892 },   // Chartres
        new PointResponse { Lat = 47.9029, Lng = 1.9039 },   // Orléans
        new PointResponse { Lat = 47.3941, Lng = 0.6848 },   // Tours
        new PointResponse { Lat = 47.2781, Lng = -0.5536 },  // Angers
        new PointResponse { Lat = 47.2184, Lng = -1.5536 },  // Nantes
    };

    public ItineraryResponse ComputeItinerary(CreateItineraryRequest request)
    {
        // Mock: always returns the hardcoded Lille → Nantes route
        // regardless of request parameters (start, end, inclination, stair)
        return new ItineraryResponse
        {
            Points = LilleToNantesRoute
        };
    }
}
