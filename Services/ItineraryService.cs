using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using AstreaEngineLib = AstreaEngine.AstreaEngine;

namespace proto_back.Services;

public class ItineraryService : IItineraryService
{
    private readonly IConfiguration _configuration;
    private readonly IGeocodingService _geocodingService;

    public ItineraryService(IConfiguration configuration, IGeocodingService geocodingService)
    {
        _configuration = configuration;
        _geocodingService = geocodingService;
    }

    public async Task<ItineraryResponse> ComputeItineraryAsync(CreateItineraryRequest request)
    {
        string graphHopperUrl = _configuration["GraphHopper:BaseUrl"]
            ?? throw new InvalidOperationException("GraphHopper:BaseUrl is not configured.");

        // Résoudre les points de départ et d'arrivée (coordonnées "lat,lng" OU nom de ville/adresse)
        var startPoint = await _geocodingService.ResolvePointAsync(request.Start);
        var endPoint = await _geocodingService.ResolvePointAsync(request.End);

        var points = new List<AstreaEngine.PointResponse>
        {
            new() { Lat = startPoint.Lat, Lng = startPoint.Lng },
            new() { Lat = endPoint.Lat, Lng = endPoint.Lng }
        };

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

}
