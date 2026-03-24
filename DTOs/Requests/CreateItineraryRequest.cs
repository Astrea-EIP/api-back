using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proto_back.DTOs.Requests;

/// <summary>
/// Type de profil de mobilité de l'utilisateur.
/// </summary>
public enum MobilityProfile
{
    Pedestrian = 0,
    Wheelchair = 1,
    Crutches = 2,
    Blind = 3
}

/// <summary>
/// Niveau de force physique — utilisé pour les personnes en fauteuil roulant.
/// Détermine la tolérance aux pentes et aux trottoirs légèrement surélevés.
/// </summary>
public enum PhysicalStrength
{
    Low = 0,  // Éviter toute pente et tout trottoir surélevé
    Medium = 1,  // Pentes légères acceptables
    High = 2   // Pentes légères + trottoirs légèrement surélevés acceptables
}

/// <summary>
/// Préférences de parcours pour les piétons valides (combinable via flags).
/// </summary>
[Flags]
public enum PathPreference
{
    None = 0,
    Stairs = 1 << 0,  // Préfère les escaliers aux pentes
    Slopes = 1 << 1,  // Préfère les pentes aux escaliers
    WidePathways = 1 << 2,  // Préfère les chemins larges
    LitStreets = 1 << 3,  // Priorité aux rues éclairées
    Benches = 1 << 4   // Bancs obligatoires sur le trajet (âge, fatigue)
}

public class CreateItineraryRequest
{
    [Required]
    [JsonPropertyName("start")]
    public string Start { get; set; } = null!;

    [Required]
    [JsonPropertyName("end")]
    public string End { get; set; } = null!;

    // -------------------------------------------------------------------------
    // Profil de mobilité
    // -------------------------------------------------------------------------

    /// <summary>
    /// Profil de mobilité de l'utilisateur.
    /// Conditionne la pertinence des autres champs.
    /// </summary>
    [Required]
    [JsonPropertyName("mobility_profile")]
    public MobilityProfile MobilityProfile { get; set; } = MobilityProfile.Pedestrian;

    // -------------------------------------------------------------------------
    // Wheelchair — champs spécifiques
    // -------------------------------------------------------------------------

    /// <summary>
    /// Largeur du fauteuil roulant en mètres.
    /// Le chemin retourné sera forcément plus large que cette valeur.
    /// </summary>
    [Range(double.Epsilon, 2.0)]
    [JsonPropertyName("wheelchair_width")]
    public double? WheelchairWidth { get; set; }

    /// <summary>
    /// Force physique de l'utilisateur en fauteuil.
    /// Détermine la tolérance aux pentes et aux trottoirs légèrement surélevés.
    /// </summary>
    [JsonPropertyName("physical_strength")]
    public PhysicalStrength? PhysicalStrength { get; set; }

    // -------------------------------------------------------------------------
    // Champ partagé : Wheelchair + Blind
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indique si l'utilisateur est accompagné.
    /// - Wheelchair : permet les pentes légères même sans force physique.
    /// - Blind      : si non accompagné, le chemin podotactile est obligatoire.
    /// </summary>
    [JsonPropertyName("is_accompanied")]
    public bool? IsAccompanied { get; set; }

    // -------------------------------------------------------------------------
    // Crutches — champs spécifiques
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indique si la personne en béquilles peut monter des escaliers.
    /// - true  : escaliers acceptables (mais pas trop nombreux/hauts).
    /// - false : pas d'escaliers, pas de trottoirs surélevés.
    /// </summary>
    [JsonPropertyName("can_climb_stairs")]
    public bool? CanClimbStairs { get; set; }

    // -------------------------------------------------------------------------
    // Pedestrian — champs spécifiques
    // -------------------------------------------------------------------------

    /// <summary>
    /// Combinaison de préférences de parcours pour un piéton valide.
    /// Exemple : PathPreference.LitStreets | PathPreference.Benches
    /// </summary>
    [JsonPropertyName("path_preferences")]
    public PathPreference? PathPreferences { get; set; }
}