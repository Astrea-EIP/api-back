using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Auth;
using proto_back.Tests.Support;

namespace proto_back.Tests.Integration;

/// <summary>
/// Sign-up and sign-in through the full HTTP pipeline, with the accounts kept by
/// <see cref="InMemoryUserRepository"/> (no MongoDB).
/// </summary>
[Trait("Category", "Integration")]
public class AccountsEndToEndTests
{
    private static object NewAccount(string email = "camille@example.com") => new
    {
        name = "Camille",
        email,
        password = "correct-horse-battery-9",
        birth_date = "1998-04-12"
    };

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal(new HashSet<string> { "accessToken" }, body.Select(kvp => kvp.Key).ToHashSet());
        return body["accessToken"]!.GetValue<string>();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var error = body["error"]?.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(error));
        return error!;
    }

    [Fact]
    public async Task Register_NewAccount_Returns201WithAUserTokenForTheStoredAccount()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = await ReadTokenAsync(response);
        var user = Assert.Single(ctx.Factory.UserRepository.Users);
        var authService = ctx.Factory.Services.GetRequiredService<IAuthService>();
        var principal = await authService.ValidateTokenAsync(token);
        Assert.NotNull(principal);
        Assert.Equal(user.Id, principal!.GetUserId());
    }

    [Fact]
    public async Task Register_NoAccessTokenHeader_IsAccepted()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_UserToken_IsAcceptedOnTheOtherRoutes()
    {
        using var ctx = ApiTestContext.Create();
        var token = await ReadTokenAsync(await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount()));
        ctx.Client.DefaultRequestHeaders.Add("access-token", token);

        var response = await ctx.Client.GetAsync("/v0/wheelchairs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmailAlreadyUsedWithAnotherCase_Returns409()
    {
        using var ctx = ApiTestContext.Create();
        await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount("camille@example.com"));

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount("Camille@Example.COM"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await ReadErrorAsync(response);
        Assert.Single(ctx.Factory.UserRepository.Users);
    }

    [Theory]
    [InlineData("motdepasse1")]
    [InlineData("élève2026")]
    [InlineData("12345678a")]
    public async Task Register_PasswordWithALetterAndADigit_IsAccepted(string password)
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", new
        {
            name = "Camille",
            email = "camille@example.com",
            password,
            birth_date = "1998-04-12"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ConsentSentByAnOlderApp_IsIgnored()
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", new
        {
            name = "Camille",
            email = "camille@example.com",
            password = "correct-horse-battery-9",
            birth_date = "1998-04-12",
            consent = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public static IEnumerable<object[]> InvalidRegisterBodies()
    {
        yield return new object[] { "missing name", """{"email":"a@example.com","password":"correct-horse-battery-9","birth_date":"1998-04-12"}""" };
        yield return new object[] { "empty name", """{"name":"","email":"a@example.com","password":"correct-horse-battery-9","birth_date":"1998-04-12"}""" };
        yield return new object[] { "name too long", $$"""{"name":"{{new string('a', 81)}}","email":"a@example.com","password":"correct-horse-battery-9","birth_date":"1998-04-12"}""" };
        yield return new object[] { "missing email", """{"name":"Camille","password":"correct-horse-battery-9","birth_date":"1998-04-12"}""" };
        yield return new object[] { "invalid email", """{"name":"Camille","email":"not-an-email","password":"correct-horse-battery-9","birth_date":"1998-04-12"}""" };
        yield return new object[] { "missing password", """{"name":"Camille","email":"a@example.com","birth_date":"1998-04-12"}""" };
        yield return new object[] { "password too short", """{"name":"Camille","email":"a@example.com","password":"short","birth_date":"1998-04-12"}""" };
        yield return new object[] { "password without digit", """{"name":"Camille","email":"a@example.com","password":"only-letters-here","birth_date":"1998-04-12"}""" };
        yield return new object[] { "password without letter", """{"name":"Camille","email":"a@example.com","password":"12345678-90","birth_date":"1998-04-12"}""" };
        yield return new object[] { "missing birth_date", """{"name":"Camille","email":"a@example.com","password":"correct-horse-battery-9"}""" };
        yield return new object[] { "birth_date in the future", """{"name":"Camille","email":"a@example.com","password":"correct-horse-battery-9","birth_date":"2999-01-01"}""" };
        yield return new object[] { "birth_date not a date", """{"name":"Camille","email":"a@example.com","password":"correct-horse-battery-9","birth_date":"12/04/1998"}""" };
        yield return new object[] { "malformed json", "{this is not json" };
    }

    [Theory]
    [MemberData(nameof(InvalidRegisterBodies))]
    public async Task Register_InvalidBody_Returns400WithNonEmptyErrorAndCreatesNothing(string scenario, string json)
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsync(
            "/v0/auth/register", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Scenario '{scenario}' returned {(int)response.StatusCode}.");
        await ReadErrorAsync(response);
        Assert.Empty(ctx.Factory.UserRepository.Users);
    }

    [Fact]
    public async Task Login_RightCredentialsWithAnotherEmailCase_Returns200WithAUserTokenForTheAccount()
    {
        using var ctx = ApiTestContext.Create();
        await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());
        var user = Assert.Single(ctx.Factory.UserRepository.Users);

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "CAMILLE@example.com", password = "correct-horse-battery-9" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await ReadTokenAsync(response);
        var principal = await ctx.Factory.Services.GetRequiredService<IAuthService>().ValidateTokenAsync(token);
        Assert.Equal(user.Id, principal!.GetUserId());
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_Return401WithTheSameMessage()
    {
        using var ctx = ApiTestContext.Create();
        await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());

        var wrongPassword = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "camille@example.com", password = "wrong-password" });
        var unknownEmail = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "nobody@example.com", password = "correct-horse-battery-9" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await ReadErrorAsync(wrongPassword), await ReadErrorAsync(unknownEmail));
    }

    [Theory]
    [InlineData("""{"password":"correct-horse-battery-9"}""")]
    [InlineData("""{"email":"camille@example.com"}""")]
    [InlineData("{this is not json")]
    public async Task Login_InvalidBody_Returns400(string json)
    {
        using var ctx = ApiTestContext.Create();

        var response = await ctx.Client.PostAsync(
            "/v0/auth/login", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ReadErrorAsync(response);
    }

    [Fact]
    public async Task GetSwaggerDocument_AccountRoutes_ArePublicWithTheAgreedStatusCodes()
    {
        using var ctx = ApiTestContext.Create();

        var doc = JsonNode.Parse(await ctx.Client.GetStringAsync("/swagger/v1/swagger.json"))!.AsObject();
        var paths = doc["paths"]!.AsObject();

        var register = paths["/v0/auth/register"]!["post"]!.AsObject();
        Assert.Equal(new HashSet<string> { "201", "400", "409", "429", "500" }, register["responses"]!.AsObject().Select(kvp => kvp.Key).ToHashSet());
        Assert.Null(register["security"]);

        var login = paths["/v0/auth/login"]!["post"]!.AsObject();
        Assert.Equal(new HashSet<string> { "200", "400", "401", "429", "500" }, login["responses"]!.AsObject().Select(kvp => kvp.Key).ToHashSet());
        Assert.Null(login["security"]);
    }

    [Fact]
    public async Task Login_MoreThanFivePerMinuteFromOneIp_Returns429WithRetryAfter()
    {
        using var ctx = ApiTestContext.Create();
        await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());

        for (var i = 0; i < 5; i++)
        {
            var allowed = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = $"user{i}@example.com", password = "wrong-password-1" });
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "camille@example.com", password = "correct-horse-battery-9" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        await ReadErrorAsync(response);
    }

    [Fact]
    public async Task Register_MoreThanTenPerHourFromOneIp_Returns429WithRetryAfter()
    {
        using var ctx = ApiTestContext.Create();

        for (var i = 0; i < 10; i++)
        {
            var allowed = await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount($"user{i}@example.com"));
            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        }

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount("one-more@example.com"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        await ReadErrorAsync(response);
        Assert.Equal(10, ctx.Factory.UserRepository.Users.Count);
    }

    [Fact]
    public async Task Login_TooManyFailuresForOneEmail_Returns429EvenWithTheRightPassword()
    {
        using var ctx = ApiTestContext.Create();
        await ctx.Client.PostAsJsonAsync("/v0/auth/register", NewAccount());
        var tracker = ctx.Factory.Services.GetRequiredService<ILoginAttemptTracker>();
        for (var i = 0; i < 10; i++)
        {
            tracker.RecordFailure("camille@example.com");
        }

        var response = await ctx.Client.PostAsJsonAsync("/v0/auth/login", new { email = "camille@example.com", password = "correct-horse-battery-9" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task OtherRoutes_AreNotRateLimited()
    {
        using var ctx = ApiTestContext.Create();

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await ctx.Client.GetAsync("/v0/auth/anonymous")).StatusCode);
        }
    }
}
