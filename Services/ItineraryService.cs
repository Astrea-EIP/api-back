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
    /// Construit le profil utilisateur envoyé au moteur (core-moteur), dans le format qu'il attend.
    /// Le preset de base vient de <c>profile</c> (walking, wheelchair, crutches, blind) ;
    /// le moteur applique ses valeurs par défaut puis les éventuelles surcharges.
    /// </summary>
    internal static string BuildUserJson(CreateItineraryRequest request)
    {
        var userObject = new JsonObject
        {
            ["profile"] = ToEnginePreset(request.MobilityProfile),
        };

        // Largeur du fauteuil (mètres) : remplace la largeur par défaut du preset fauteuil.
        if (request.MobilityProfile == MobilityProfile.Wheelchair && request.WheelchairWidth.HasValue)
        {
            userObject["width"] = request.WheelchairWidth.Value;
        }

        return userObject.ToJsonString();
    }

    /// <summary>
    /// Nom du preset moteur correspondant au profil de mobilité de l'API.
    /// </summary>
    internal static string ToEnginePreset(MobilityProfile profile) => profile switch
    {
        MobilityProfile.Pedestrian => "walking",
        MobilityProfile.Wheelchair => "wheelchair",
        MobilityProfile.Crutches => "crutches",
        MobilityProfile.Blind => "blind",
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown mobility profile."),
    };

}
