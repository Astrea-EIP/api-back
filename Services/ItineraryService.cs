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

        // Appel au moteur Astrea : les erreurs GraphHopper connues deviennent des erreurs client lisibles.
        List<AstreaEngine.PointResponse> astreaRouteResult;
        try
        {
            astreaRouteResult = await AstreaEngineLib.AstreaRouteAsync(graphHopperUrl, points, userJson);
        }
        catch (InvalidOperationException ex) when (GraphHopperErrorTranslator.TryTranslate(ex, out var apiException))
        {
            throw apiException!;
        }

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

        // Béquilles : peut monter des escaliers ou non.
        if (request.MobilityProfile == MobilityProfile.Crutches && request.CanClimbStairs.HasValue)
        {
            userObject["can_take_stairs"] = request.CanClimbStairs.Value;
        }

        // Piéton : "préfère les escaliers aux pentes" => éviter les pentes,
        // "préfère les pentes aux escaliers" => éviter les escaliers.
        // Les deux à la fois se contredisent : aucune préférence n'est envoyée.
        if (request.MobilityProfile == MobilityProfile.Pedestrian && request.PathPreferences.HasValue)
        {
            var preferences = request.PathPreferences.Value;
            var prefersStairs = preferences.HasFlag(PathPreference.Stairs);
            var prefersSlopes = preferences.HasFlag(PathPreference.Slopes);

            if (prefersStairs && !prefersSlopes)
            {
                userObject["avoid_slopes"] = true;
            }
            else if (prefersSlopes && !prefersStairs)
            {
                userObject["avoid_stairs"] = true;
            }
        }

        // TODO: champs acceptés par l'API mais pas encore gérés par le moteur, donc non transmis.
        // A brancher quand core-moteur exposera une clé pour chacun :
        // - PhysicalStrength (fauteuil) : tolérance aux pentes et aux trottoirs surélevés ;
        // - IsAccompanied (fauteuil, malvoyant) ;
        // - PathPreferences WidePathways, LitStreets, Benches (piéton).

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
