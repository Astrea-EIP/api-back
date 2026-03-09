using System;
using System.Collections.Generic;
using System.Globalization;
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

    public ItineraryResponse ComputeItinerary(CreateItineraryRequest request)
    {
        string? graphHopperUrl = _configuration["GraphHopper:BaseUrl"];
        if (string.IsNullOrEmpty(graphHopperUrl))
        {
            throw new InvalidOperationException("GraphHopper:BaseUrl is not configured.");
        }

        var startPoint = ParseCoordinate(request.Start);
        var endPoint = ParseCoordinate(request.End);

        var points = new List<AstreaEngine.PointResponse> { startPoint, endPoint };

        string profileName = request.MobilityProfile switch
        {
            MobilityProfile.Pedestrian => "foot",
            MobilityProfile.Wheelchair => "wheelchair",
            MobilityProfile.Crutches => "crutches",
            MobilityProfile.Blind => "blind",
            _ => "foot"
        };

        string userJson = $"{{\"profile\":\"{profileName}\"}}";

        var astreaRouteResult = AstreaEngineLib.AstreaRoute(graphHopperUrl, points, userJson);

        var responsePoints = new List<proto_back.DTOs.Responses.PointResponse>();
        foreach (var p in astreaRouteResult)
        {
            responsePoints.Add(new proto_back.DTOs.Responses.PointResponse
            {
                Lat = p.Lat,
                Lng = p.Lng
            });
        }

        return new ItineraryResponse
        {
            Points = responsePoints
        };
    }

    private AstreaEngine.PointResponse ParseCoordinate(string coordinate)
    {
        if (string.IsNullOrWhiteSpace(coordinate))
        {
            throw new ArgumentException("Invalid coordinate format. Expected 'lat,lng' (e.g. '47.2184,-1.5536').");
        }

        var parts = coordinate.Split(',');
        if (parts.Length != 2 || 
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lng))
        {
            throw new ArgumentException("Invalid coordinate format. Expected 'lat,lng' (e.g. '47.2184,-1.5536').");
        }

        return new AstreaEngine.PointResponse
        {
            Lat = lat,
            Lng = lng
        };
    }
}
