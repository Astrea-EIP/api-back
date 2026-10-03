using System.Globalization;

namespace proto_back.Tests.Support;

/// <summary>
/// Temporarily swaps <see cref="CultureInfo.CurrentCulture"/>/<see cref="CultureInfo.CurrentUICulture"/>
/// and restores the previous values on dispose. Lets tests characterize culture-sensitive
/// parsing/formatting regardless of the host machine's locale.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previousCulture;
    private readonly CultureInfo _previousUiCulture;

    public CultureScope(string cultureName)
    {
        _previousCulture = CultureInfo.CurrentCulture;
        _previousUiCulture = CultureInfo.CurrentUICulture;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}
