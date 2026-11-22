using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using proto_back.Tests.Support;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace proto_back.Tests.Integration;

/// <summary>
/// The <c>/v0/users/me</c> routes through the full HTTP pipeline, with the accounts and
/// profiles kept by the in-memory repositories (no MongoDB).
/// </summary>
[Trait("Category", "Integration")]
public class UsersEndToEndTests
{
    private const string Route = """{"paths":[{"points":{"coordinates":[[2.3522,48.8566],[2.3376,48.8606]]}}]}""";

    private const string WheelchairProfile =
        """{"consent":true,"app_profile":"manual-wheelchair","mobility_profile":1,"wheelchair_model_id":"tilite-active","wheelchair_width":0.7,"physical_strength":1,"is_accompanied":true,"can_climb_stairs":true,"path_preferences":24}""";

    private static async Task<string> RegisterAsync(ApiTestContext ctx, string email = "camille@example.com")
    {
        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", new
        {
            name = "Camille",
            email,
            password = "correct-horse-battery",
            birth_date = "1998-04-12"
        });
        response.EnsureSuccessStatusCode();
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return body["accessToken"]!.GetValue<string>();
    }

    private static Task<HttpResponseMessage> SendAsync(ApiTestContext ctx, HttpMethod method, string path, string? token, string? json = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Add("access-token", token);
        }

        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return ctx.Client.SendAsync(request);
    }

    private static async Task<JsonObject> ReadObjectAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();

    public static IEnumerable<object[]> Routes()
    {
        yield return new object[] { "GET", "/v0/users/me" };
        yield return new object[] { "DELETE", "/v0/users/me" };
        yield return new object[] { "GET", "/v0/users/me/profile" };
        yield return new object[] { "PUT", "/v0/users/me/profile" };
        yield return new object[] { "DELETE", "/v0/users/me/profile" };
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task AnyRoute_NoToken_Returns401(string method, string path)
    {
        using var ctx = ApiTestContext.Create();

        var response = await SendAsync(ctx, new HttpMethod(method), path, null, method == "PUT" ? WheelchairProfile : null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task AnyRoute_AnonymousToken_Returns401(string method, string path)
    {
        using var ctx = ApiTestContext.Create();
        var token = await ctx.Client.GetAnonymousTokenAsync();

        var response = await SendAsync(ctx, new HttpMethod(method), path, token, method == "PUT" ? WheelchairProfile : null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task AnyRoute_TokenOfADeletedAccount_Returns401(string method, string path)
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me", token)).StatusCode);

        var response = await SendAsync(ctx, new HttpMethod(method), path, token, method == "PUT" ? WheelchairProfile : null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(ctx.Factory.UserProfileRepository.Profiles);
    }

    [Fact]
    public async Task TokenOfADeletedAccount_StillWorksOnTheAnonymousRoutes()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);
        await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me", token);

        var response = await SendAsync(ctx, HttpMethod.Get, "/v0/wheelchairs", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_UserToken_Returns200WithTheAccountAndNoPassword()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx, "Camille@Example.com");
        var user = Assert.Single(ctx.Factory.UserRepository.Users);

        var response = await SendAsync(ctx, HttpMethod.Get, "/v0/users/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadObjectAsync(response);
        Assert.Equal(new HashSet<string> { "id", "name", "email", "birth_date", "created_at" }, body.Select(kvp => kvp.Key).ToHashSet());
        Assert.Equal(user.Id, body["id"]!.GetValue<string>());
        Assert.Equal("camille@example.com", body["email"]!.GetValue<string>());
        Assert.Equal("1998-04-12", body["birth_date"]!.GetValue<string>());
    }

    [Fact]
    public async Task DeleteMe_UserToken_Returns204AndDeletesTheAccountAndTheProfile()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);
        await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", token, WheelchairProfile);

        var response = await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(ctx.Factory.UserRepository.Users);
        Assert.Empty(ctx.Factory.UserProfileRepository.Profiles);
        var login = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "camille@example.com", password = "correct-horse-battery" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_OnlyDeletesTheAccountOfTheToken()
    {
        using var ctx = ApiTestContext.Create();
        var camille = await RegisterAsync(ctx, "camille@example.com");
        var other = await RegisterAsync(ctx, "other@example.com");
        await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", other, WheelchairProfile);

        await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me", camille);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(ctx, HttpMethod.Get, "/v0/users/me/profile", other)).StatusCode);
    }

    [Fact]
    public async Task GetProfile_NoProfileSaved_Returns404WithError()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);

        var response = await SendAsync(ctx, HttpMethod.Get, "/v0/users/me/profile", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(string.IsNullOrEmpty((await ReadObjectAsync(response))["error"]?.GetValue<string>()));
    }

    [Fact]
    public async Task PutProfile_Wheelchair_Returns200WithOnlyTheWheelchairFieldsThenGetReturnsTheSame()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);

        var put = await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", token, WheelchairProfile);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var saved = await ReadObjectAsync(put);
        Assert.Equal(
            new HashSet<string>
            {
                "consent", "app_profile", "mobility_profile", "wheelchair_model_id", "wheelchair_width",
                "physical_strength", "is_accompanied", "consent_given_at", "updated_at"
            },
            saved.Select(kvp => kvp.Key).ToHashSet());
        Assert.True(saved["consent"]!.GetValue<bool>());
        Assert.Equal(1, saved["mobility_profile"]!.GetValue<int>());
        Assert.Equal(0.7, saved["wheelchair_width"]!.GetValue<double>());

        var get = await SendAsync(ctx, HttpMethod.Get, "/v0/users/me/profile", token);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(saved.ToJsonString(), (await ReadObjectAsync(get)).ToJsonString());
    }

    [Fact]
    public async Task PutProfile_SavedProfile_CanBeSentAsIsAsAnItineraryRequest()
    {
        using var ctx = ApiTestContext.Create();
        ctx.GraphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(Route));
        var token = await RegisterAsync(ctx);
        var saved = await ReadObjectAsync(await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", token, WheelchairProfile));

        var itinerary = saved.DeepClone().AsObject();
        itinerary["start"] = "48.8566,2.3522";
        itinerary["end"] = "48.8606,2.3376";
        var response = await SendAsync(ctx, HttpMethod.Post, "/v0/itinerary", token, itinerary.ToJsonString());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public static IEnumerable<object[]> InvalidProfiles()
    {
        yield return new object[] { "consent missing", """{"mobility_profile":0}""" };
        yield return new object[] { "consent false", """{"consent":false,"mobility_profile":0}""" };
        yield return new object[] { "mobility_profile missing", """{"consent":true}""" };
        yield return new object[] { "mobility_profile out of range", """{"consent":true,"mobility_profile":9}""" };
        yield return new object[] { "physical_strength out of range", """{"consent":true,"mobility_profile":1,"physical_strength":7}""" };
        yield return new object[] { "path_preferences out of range", """{"consent":true,"mobility_profile":0,"path_preferences":64}""" };
        yield return new object[] { "wheelchair_width zero", """{"consent":true,"mobility_profile":1,"wheelchair_width":0}""" };
        yield return new object[] { "wheelchair_width too large", """{"consent":true,"mobility_profile":1,"wheelchair_width":2.5}""" };
        yield return new object[] { "unknown wheelchair model", """{"consent":true,"mobility_profile":1,"wheelchair_model_id":"no-such-model"}""" };
        yield return new object[] { "app_profile too long", $$"""{"consent":true,"mobility_profile":0,"app_profile":"{{new string('a', 41)}}"}""" };
        yield return new object[] { "malformed json", "{this is not json" };
    }

    [Theory]
    [MemberData(nameof(InvalidProfiles))]
    public async Task PutProfile_InvalidBody_Returns400AndStoresNothing(string scenario, string json)
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);

        var response = await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", token, json);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Scenario '{scenario}' returned {(int)response.StatusCode}.");
        Assert.False(string.IsNullOrEmpty((await ReadObjectAsync(response))["error"]?.GetValue<string>()));
        Assert.Empty(ctx.Factory.UserProfileRepository.Profiles);
    }

    [Fact]
    public async Task DeleteProfile_SavedProfile_Returns204DeletesItAndKeepsTheAccount()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);
        await SendAsync(ctx, HttpMethod.Put, "/v0/users/me/profile", token, WheelchairProfile);

        var response = await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me/profile", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(ctx.Factory.UserProfileRepository.Profiles);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(ctx, HttpMethod.Get, "/v0/users/me/profile", token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(ctx, HttpMethod.Get, "/v0/users/me", token)).StatusCode);
    }

    [Fact]
    public async Task DeleteProfile_NoProfile_Returns204()
    {
        using var ctx = ApiTestContext.Create();
        var token = await RegisterAsync(ctx);

        var response = await SendAsync(ctx, HttpMethod.Delete, "/v0/users/me/profile", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerDocument_UserRoutes_NeedATokenAndHaveTheAgreedStatusCodes()
    {
        using var ctx = ApiTestContext.Create();

        var doc = JsonNode.Parse(await ctx.Client.GetStringAsync("/swagger/v1/swagger.json"))!.AsObject();
        var paths = doc["paths"]!.AsObject();

        var expected = new Dictionary<(string, string), string[]>
        {
            [("/v0/users/me", "get")] = new[] { "200", "401", "500" },
            [("/v0/users/me", "delete")] = new[] { "204", "401", "500" },
            [("/v0/users/me/profile", "get")] = new[] { "200", "401", "404", "500" },
            [("/v0/users/me/profile", "put")] = new[] { "200", "400", "401", "500" },
            [("/v0/users/me/profile", "delete")] = new[] { "204", "401", "500" }
        };
        foreach (var ((path, method), codes) in expected)
        {
            var operation = paths[path]![method]!.AsObject();
            Assert.Equal(codes.ToHashSet(), operation["responses"]!.AsObject().Select(kvp => kvp.Key).ToHashSet());
            Assert.NotNull(operation["security"]);
        }
    }
}
