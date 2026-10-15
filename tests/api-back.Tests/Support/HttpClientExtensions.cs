using System.Net.Http.Json;

namespace proto_back.Tests.Support;

public static class HttpClientExtensions
{
    public static async Task<string> GetAnonymousTokenAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/v0/auth/anonymous");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return body!["accessToken"];
    }
}
