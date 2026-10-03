using WireMock.Server;
using WireMock.Settings;

namespace proto_back.Tests.Support;

/// <summary>
/// Thin wrapper around a <see cref="WireMockServer"/> bound to a random local port,
/// used to stand in for GraphHopper or Nominatim in tests. No test may reach the real
/// services, so every GraphHopper/Nominatim call in this project must go through one
/// of these.
/// </summary>
public sealed class WireMockFixture : IDisposable
{
    private bool _disposed;

    public WireMockServer Server { get; }

    public string BaseUrl => Server.Urls[0];

    public WireMockFixture()
    {
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            StartAdminInterface = false
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Server.Stop();
        Server.Dispose();
    }
}
