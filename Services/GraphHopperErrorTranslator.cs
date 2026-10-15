using System.Text.RegularExpressions;
using proto_back.Shared.Errors;

namespace proto_back.Services;

/// <summary>
/// Turns the GraphHopper errors relayed by the engine into client errors with a readable message.
/// GraphHopper answers these cases with a 400 whose body names the Java exception in
/// <c>hints[].details</c>; the engine copies that body into its exception message.
/// </summary>
internal static partial class GraphHopperErrorTranslator
{
    private static readonly string[] PointErrors =
    {
        "PointOutOfBoundsException",
        "PointNotFoundException",
    };

    private static readonly string[] RouteErrors =
    {
        "ConnectionNotFoundException",
        "MaximumNodesExceededException",
    };

    /// <summary>
    /// Returns true and the client error to throw when <paramref name="exception"/> is a known
    /// GraphHopper error; false for anything else, which stays an unexpected error.
    /// </summary>
    public static bool TryTranslate(Exception exception, out ApiException? apiException)
    {
        var message = exception.Message;

        if (PointErrors.Any(name => message.Contains(name, StringComparison.Ordinal)))
        {
            apiException = new PointOutOfMapException(DescribePoint(message), exception);
            return true;
        }

        if (RouteErrors.Any(name => message.Contains(name, StringComparison.Ordinal)))
        {
            apiException = new RouteNotFoundException(
                "Aucun trajet adapté à ce profil de mobilité n'a été trouvé entre ces deux points.",
                exception);
            return true;
        }

        apiException = null;
        return false;
    }

    private static string DescribePoint(string message)
    {
        var match = PointIndexRegex().Match(message);
        var point = match.Success
            ? match.Groups[1].Value switch
            {
                "0" => "Le point de départ",
                "1" => "Le point d'arrivée",
                _ => "Un des points du trajet",
            }
            : "Un des points du trajet";

        return $"{point} est en dehors de la zone couverte par la carte, ou trop loin d'un chemin.";
    }

    [GeneratedRegex("\"point_index\"\\s*:\\s*(\\d+)")]
    private static partial Regex PointIndexRegex();
}
