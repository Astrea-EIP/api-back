using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using AstreaEngineLib = AstreaEngine.AstreaEngine;

namespace proto_back.Services;

public class ItineraryService : IItineraryService
{
    private readonly IConfiguration _configuration;

    public ItineraryService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ItineraryResponse> ComputeItineraryAsync(CreateItineraryRequest request)
    {
        string graphHopperUrl = _configuration["GraphHopper:BaseUrl"]
            ?? throw new InvalidOperationException("GraphHopper:BaseUrl is not configured.");

        string nominatimUrl = _configuration["Nominatim:BaseUrl"]
            ?? throw new InvalidOperationException("Nominatim:BaseUrl is not configured.");

        // Résoudre les points de départ et d'arrivée (coordonnées "lat,lng" OU nom de ville/adresse)
        var startPoint = await ResolvePointAsync(nominatimUrl, request.Start);
        var endPoint = await ResolvePointAsync(nominatimUrl, request.End);

        var points = new List<AstreaEngine.PointResponse> { startPoint, endPoint };

        // Construire le JSON utilisateur avec tous les paramètres disponibles
        string userJson = BuildUserJson(request);

        // Appel au moteur Astrea
        var astreaRouteResult = await AstreaEngineLib.AstreaRouteAsync(graphHopperUrl, points, userJson);

        // Mapper le résultat vers les DTOs de réponse
        var responsePoints = astreaRouteResult
            .Select(p => new proto_back.DTOs.Responses.PointResponse
            {
                Lat = p.Lat,
                Lng = p.Lng
            })
            .ToList();

        return new ItineraryResponse
        {
            Points = responsePoints
        };
    }

    /// <summary>
    /// Construit le payload JSON utilisateur avec tous les paramètres de la requête.
    /// Le moteur attend : mobility_profile (int), path_preferences (int bitmask).
    /// Les champs additionnels sont inclus pour compatibilité future.
    /// </summary>
    private static string BuildUserJson(CreateItineraryRequest request)
    {
        var userObject = new JsonObject
        {
            ["mobility_profile"] = (int)request.MobilityProfile,
        };

        // Préférences de parcours (bitmask : escaliers, pentes, chemins larges, éclairage, bancs)
        if (request.PathPreferences.HasValue)
        {
            userObject["path_preferences"] = (int)request.PathPreferences.Value;
        }

        // Largeur du fauteuil roulant (mètres)
        if (request.WheelchairWidth.HasValue)
        {
            userObject["wheelchair_width"] = request.WheelchairWidth.Value;
        }

        // Force physique (0=Low, 1=Medium, 2=High)
        if (request.PhysicalStrength.HasValue)
        {
            userObject["physical_strength"] = (int)request.PhysicalStrength.Value;
        }

        // Accompagné (impacte Wheelchair et Blind)
        if (request.IsAccompanied.HasValue)
        {
            userObject["is_accompanied"] = request.IsAccompanied.Value;
        }

        // Peut monter des escaliers (Crutches)
        if (request.CanClimbStairs.HasValue)
        {
            userObject["can_climb_stairs"] = request.CanClimbStairs.Value;
        }

        return userObject.ToJsonString();
    }

    /// <summary>
    /// Résout un point à partir d'une chaîne de coordonnées "lat,lng"
    /// ou d'un nom d'adresse/ville via AstreaAdressToCoordinates.
    /// </summary>
    private static async Task<AstreaEngine.PointResponse> ResolvePointAsync(string nominatimUrl, string input)
    {
        // Tenter d'abord l'interprétation comme coordonnées
        if (TryParseCoordinate(input, out var point))
        {
            return point;
        }

        // Sinon, traiter comme un nom d'adresse/ville et géocoder via Nominatim
        var results = await AstreaEngineLib.AstreaAdressToCoordinatesAsync(nominatimUrl, input, 1);

        if (results.Count == 0)
        {
            throw new ArgumentException(
                $"Impossible de résoudre l'adresse '{input}' en coordonnées. "
                + "Vérifiez l'orthographe ou utilisez le format 'lat,lng'.");
        }

        // Prendre le premier résultat
        return results.Values.First();
    }

    /// <summary>
    /// Tente de parser une chaîne au format "lat,lng".
    /// </summary>
    private static bool TryParseCoordinate(string input, out AstreaEngine.PointResponse point)
    {
        point = new AstreaEngine.PointResponse();

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
            double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lng))
        {
            point.Lat = lat;
            point.Lng = lng;
            return true;
        }

        return false;
    }
}
