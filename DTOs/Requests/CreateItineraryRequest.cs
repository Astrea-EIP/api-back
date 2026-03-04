using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proto_back.DTOs.Requests;

public class CreateItineraryRequest
{
    [Required]
    [JsonPropertyName("start")]
    public string Start { get; set; } = null!;

    [Required]
    [JsonPropertyName("end")]
    public string End { get; set; } = null!;

    [Range(double.Epsilon, 90)]
    [JsonPropertyName("inclination_max")]
    public double? InclinationMax { get; set; }

    [JsonPropertyName("stair")]
    public bool Stair { get; set; } = true;
}
