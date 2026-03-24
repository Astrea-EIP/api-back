using System.Globalization;
using System.Net.Http.Json;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PointResponse> ResolvePointAsync(string input)
    {
        if (TryParseCoordinate(input, out var coordinatePoint))
        {
            return coordinatePoint;
        }

        var encodedQuery = Uri.EscapeDataString(input);
        var requestPath = $"search?q={encodedQuery}&format=json&limit=1";

        var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(requestPath);

        if (results is null || results.Count == 0)
        {
            throw new ArgumentException(
                $"Impossible de résoudre l'adresse '{input}' en coordonnées. "
                + "Vérifiez l'orthographe ou utilisez le format 'lat,lng'.");
        }

        if (!double.TryParse(results[0].Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(results[0].Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            throw new InvalidOperationException($"Coordonnées invalides retournées par le geocoding pour '{input}'.");
        }

        return new PointResponse
        {
            Lat = lat,
            Lng = lng
        };
    }

    private static bool TryParseCoordinate(string input, out PointResponse point)
    {
        point = new PointResponse();

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            return false;
        }

        point.Lat = lat;
        point.Lng = lng;
        return true;
    }

    private sealed class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;

        public string Lon { get; set; } = string.Empty;
    }
}
