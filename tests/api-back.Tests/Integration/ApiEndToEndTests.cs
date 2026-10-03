using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using proto_back.Tests.Support;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace proto_back.Tests.Integration;

/// <summary>
/// Full HTTP pipeline tests through <see cref="ApiFactory"/>: real routing, model
/// binding, middlewares and the real AstreaEngine.dll, with GraphHopper and Nominatim
/// simulated by WireMock. No test here reaches a real network host.
/// </summary>
[Trait("Category", "Integration")]
public class ApiEndToEndTests
{
    private const string Route = """{"paths":[{"points":{"coordinates":[[2.3522,48.8566],[2.3376,48.8606]]}}]}""";

    [Fact]
    public async Task GetAnonymousToken_Always_Returns202WithOnlyAccessTokenProperty()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.GetAsync("/v0/auth/anonymous");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal(new HashSet<string> { "accessToken" }, body.Select(kvp => kvp.Key).ToHashSet());
        Assert.Matches("^[0-9a-f]{32}$", body["accessToken"]!.GetValue<string>());
    }

    [Fact]
    public async Task PostItinerary_NoHeader_Returns401WithEmptyBody()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new { start = "a", end = "b", mobility_profile = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task PostItinerary_InvalidToken_Returns401()
    {
        using var ctx = ApiTestContext.Create();
        ctx.Client.DefaultRequestHeaders.Add("access-token", "not-a-real-token");

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new { start = "a", end = "b", mobility_profile = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static IEnumerable<object[]> InvalidBodies()
    {
        yield return new object[] { "missing start", """{"end":"48.8606,2.3376","mobility_profile":0}""" };
        yield return new object[] { "missing end", """{"start":"48.8566,2.3522","mobility_profile":0}""" };
        yield return new object[] { "wheelchair_width zero", """{"start":"a","end":"b","mobility_profile":1,"wheelchair_width":0}""" };
        yield return new object[] { "wheelchair_width too large", """{"start":"a","end":"b","mobility_profile":1,"wheelchair_width":2.5}""" };
        yield return new object[] { "malformed json", "{this is not json" };
        yield return new object[] { "mobility_profile as string", """{"start":"a","end":"b","mobility_profile":"Wheelchair"}""" };
    }

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task PostItinerary_InvalidBody_Returns400WithNonEmptyError(string scenario, string json)
    {
        using var ctx = ApiTestContext.Create();
        var token = await ctx.Client.GetAnonymousTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v0/itinerary")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("access-token", token);

        var response = await ctx.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.False(string.IsNullOrEmpty(body["error"]?.GetValue<string>()), $"Scenario '{scenario}' expected a non-empty error.");
    }

    [Fact]
    public async Task PostItinerary_Coordinates_Returns201AndSendsStartThenEndToGraphHopper()
    {
        using var ctx = ApiTestContext.Create();
        ctx.GraphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(Route));

        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "48.8566,2.3522",
            end = "48.8606,2.3376",
            mobility_profile = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var points = body["points"]!.AsArray();
        Assert.Equal(2, points.Count);

        var requestBody = JsonNode.Parse(ctx.GraphHopper.Server.LogEntries.Single().RequestMessage.Body!)!.AsObject();
        var sentPoints = requestBody["points"]!.AsArray();
        Assert.Equal(2.3522, sentPoints[0]![0]!.GetValue<double>());
        Assert.Equal(48.8566, sentPoints[0]![1]!.GetValue<double>());
        Assert.Equal(2.3376, sentPoints[1]![0]!.GetValue<double>());
        Assert.Equal(48.8606, sentPoints[1]![1]!.GetValue<double>());
    }

    [Fact]
    public async Task PostItinerary_Addresses_HitsNominatimWithExpectedQueryAndUserAgentThenReturns201()
    {
        using var ctx = ApiTestContext.Create();
        ctx.Nominatim.Server
            .Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""[{"lat":"48.8566","lon":"2.3522"}]"""));
        ctx.GraphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(Route));

        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "Place de la Concorde, Paris",
            end = "Arc de Triomphe, Paris",
            mobility_profile = 0
        });

        var nominatimRequests = ctx.Nominatim.Server.LogEntries.ToList();
        Assert.Equal(2, nominatimRequests.Count);
        Assert.Contains("q=Place", nominatimRequests[0].RequestMessage.Url);
        Assert.Contains("Place de la Concorde, Paris", Uri.UnescapeDataString(nominatimRequests[0].RequestMessage.Url));
        Assert.StartsWith("api-back/", nominatimRequests[0].RequestMessage.Headers!["User-Agent"].First());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostItinerary_GraphHopperFails_Returns500AndPersistsErrorLogWithRequestContext()
    {
        using var ctx = ApiTestContext.Create();
        ctx.GraphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("graphhopper down"));

        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "48.8566,2.3522",
            end = "48.8606,2.3376",
            mobility_profile = 0
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal(new HashSet<string> { "error", "errorId" }, body.Select(kvp => kvp.Key).ToHashSet());

        var persisted = await Poll.UntilAsync(() => ctx.Factory.ErrorLogRepository.Logs.Count > 0);
        Assert.True(persisted, "Expected the error log to be persisted within the timeout.");

        var log = ctx.Factory.ErrorLogRepository.Logs.Single();
        Assert.Equal("/v0/itinerary", log.RequestPath);
        Assert.Equal("POST", log.HttpMethod);
    }

    [Fact]
    public async Task PostItinerary_NoGraphHopperBaseUrlConfigured_Returns500MentioningTheMissingKey()
    {
        using var ctx = ApiTestContext.Create(configureGraphHopper: false);
        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "48.8566,2.3522",
            end = "48.8606,2.3376",
            mobility_profile = 0
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Contains("GraphHopper:BaseUrl", body["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetSwaggerDocument_NoToken_Returns200WithExpectedOperationsAndStatusCodes()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var paths = doc["paths"]!.AsObject();

        var itinerary = paths["/v0/itinerary"]!["post"]!["responses"]!.AsObject();
        Assert.Equal(new HashSet<string> { "201", "400", "401", "500" }, itinerary.Select(kvp => kvp.Key).ToHashSet());

        var auth = paths["/v0/auth/anonymous"]!["get"]!["responses"]!.AsObject();
        Assert.Equal(new HashSet<string> { "202", "400", "500" }, auth.Select(kvp => kvp.Key).ToHashSet());
    }

    // --- Known contract violations (docs/api.json vs. actual behavior) ---
    // Written for the CORRECT behavior and skipped, per the issue's instructions:
    // fixing the application is out of scope for this test-setup PR.

    [Fact(Skip = "Contract violation: an unresolvable address should be a 400 (bad input), " +
                 "but NominatimGeocodingService throws ArgumentException which ExceptionHandlingMiddleware " +
                 "maps to a 500. See Findings in the PR description.")]
    public async Task PostItinerary_UnresolvableAddress_ShouldReturn400()
    {
        using var ctx = ApiTestContext.Create();
        ctx.Nominatim.Server
            .Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("[]"));

        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "Nowhereland",
            end = "48.8606,2.3376",
            mobility_profile = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Contract violation: docs/api.json marks mobility_profile as required, so a missing " +
                 "value should be a 400, but the enum defaults to Pedestrian (0) and [Required] never " +
                 "fires on a non-nullable value type. See Findings in the PR description.")]
    public async Task PostItinerary_MissingMobilityProfile_ShouldReturn400()
    {
        using var ctx = ApiTestContext.Create();
        ctx.GraphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(Route));

        var token = await ctx.Client.GetAnonymousTokenAsync();
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.PostAsJsonAsync("/v0/itinerary", new
        {
            start = "48.8566,2.3522",
            end = "48.8606,2.3376"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
