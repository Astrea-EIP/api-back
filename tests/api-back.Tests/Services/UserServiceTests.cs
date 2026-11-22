using Microsoft.Extensions.Time.Testing;
using MongoDB.Bson;
using proto_back.DTOs.Requests;
using proto_back.Models.Entities;
using proto_back.Services;
using proto_back.Shared.Errors;
using proto_back.Tests.Support;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 14, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryUserProfileRepository _profiles = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly UserService _service;
    private readonly User _user;

    public UserServiceTests()
    {
        _service = new UserService(_users, _profiles, new WheelchairService(), _time);
        _user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Camille",
            Email = "camille@example.com",
            BirthDate = new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash = "hash",
            CreatedAt = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc)
        };
        _users.TryCreateAsync(_user).GetAwaiter().GetResult();
    }

    private static UserProfileRequest AllFields(MobilityProfile profile) => new()
    {
        Consent = true,
        AppProfile = "some-choice",
        MobilityProfile = profile,
        WheelchairModelId = "tilite-active",
        WheelchairWidth = 0.7,
        PhysicalStrength = PhysicalStrength.High,
        IsAccompanied = true,
        CanClimbStairs = true,
        PathPreferences = PathPreference.LitStreets | PathPreference.Benches
    };

    [Fact]
    public async Task GetAccountAsync_ExistingAccount_ReturnsItWithoutThePasswordHash()
    {
        var account = await _service.GetAccountAsync(_user.Id);

        Assert.NotNull(account);
        Assert.Equal(_user.Id, account!.Id);
        Assert.Equal("Camille", account.Name);
        Assert.Equal("camille@example.com", account.Email);
        Assert.Equal(new DateOnly(1998, 4, 12), account.BirthDate);
        Assert.Equal(_user.CreatedAt, account.CreatedAt);
    }

    [Theory]
    [InlineData("6740f1c2a9e3b54d2c1f0a77")]
    [InlineData("not-an-object-id")]
    public async Task GetAccountAsync_UnknownAccount_ReturnsNull(string userId)
    {
        Assert.Null(await _service.GetAccountAsync(userId));
        Assert.False(await _service.AccountExistsAsync(userId));
    }

    [Fact]
    public async Task DeleteAccountAsync_ExistingAccount_DeletesTheAccountAndTheProfile()
    {
        await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Wheelchair));

        var deleted = await _service.DeleteAccountAsync(_user.Id);

        Assert.True(deleted);
        Assert.Empty(_users.Users);
        Assert.Empty(_profiles.Profiles);
    }

    [Fact]
    public async Task DeleteAccountAsync_UnknownAccount_ReturnsFalse()
    {
        Assert.False(await _service.DeleteAccountAsync(ObjectId.GenerateNewId().ToString()));
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task SaveProfileAsync_Wheelchair_KeepsOnlyTheWheelchairFields()
    {
        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Wheelchair));

        Assert.True(saved.Consent);
        Assert.Equal("some-choice", saved.AppProfile);
        Assert.Equal(MobilityProfile.Wheelchair, saved.MobilityProfile);
        Assert.Equal("tilite-active", saved.WheelchairModelId);
        Assert.Equal(0.7, saved.WheelchairWidth);
        Assert.Equal(PhysicalStrength.High, saved.PhysicalStrength);
        Assert.True(saved.IsAccompanied);
        Assert.Null(saved.CanClimbStairs);
        Assert.Null(saved.PathPreferences);
    }

    [Fact]
    public async Task SaveProfileAsync_Pedestrian_KeepsOnlyThePathPreferences()
    {
        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Pedestrian));

        Assert.Equal(PathPreference.LitStreets | PathPreference.Benches, saved.PathPreferences);
        Assert.Null(saved.WheelchairModelId);
        Assert.Null(saved.WheelchairWidth);
        Assert.Null(saved.PhysicalStrength);
        Assert.Null(saved.IsAccompanied);
        Assert.Null(saved.CanClimbStairs);
        var stored = Assert.Single(_profiles.Profiles);
        Assert.Null(stored.WheelchairWidth);
    }

    [Fact]
    public async Task SaveProfileAsync_Crutches_KeepsOnlyCanClimbStairs()
    {
        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Crutches));

        Assert.True(saved.CanClimbStairs);
        Assert.Null(saved.IsAccompanied);
        Assert.Null(saved.PathPreferences);
        Assert.Null(saved.WheelchairWidth);
    }

    [Fact]
    public async Task SaveProfileAsync_Blind_KeepsOnlyIsAccompanied()
    {
        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Blind));

        Assert.True(saved.IsAccompanied);
        Assert.Null(saved.CanClimbStairs);
        Assert.Null(saved.PathPreferences);
        Assert.Null(saved.PhysicalStrength);
    }

    [Fact]
    public async Task SaveProfileAsync_WheelchairModelWithoutWidth_StoresTheModelsRecommendedWidth()
    {
        var request = new UserProfileRequest { Consent = true, MobilityProfile = MobilityProfile.Wheelchair, WheelchairModelId = "TILITE-ACTIVE" };

        var saved = await _service.SaveProfileAsync(_user.Id, request);

        Assert.Equal("tilite-active", saved.WheelchairModelId);
        Assert.Equal(0.65, saved.WheelchairWidth);
    }

    [Fact]
    public async Task SaveProfileAsync_UnknownWheelchairModel_ThrowsAndStoresNothing()
    {
        var request = new UserProfileRequest { Consent = true, MobilityProfile = MobilityProfile.Wheelchair, WheelchairModelId = "no-such-model" };

        var ex = await Assert.ThrowsAsync<UnknownWheelchairModelException>(() => _service.SaveProfileAsync(_user.Id, request));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("no-such-model", ex.Message);
        Assert.Empty(_profiles.Profiles);
    }

    [Fact]
    public async Task SaveProfileAsync_UnknownWheelchairModelOnAnotherProfile_IsIgnored()
    {
        var request = new UserProfileRequest { Consent = true, MobilityProfile = MobilityProfile.Pedestrian, WheelchairModelId = "no-such-model" };

        var saved = await _service.SaveProfileAsync(_user.Id, request);

        Assert.Null(saved.WheelchairModelId);
    }

    [Fact]
    public async Task SaveProfileAsync_Replacement_KeepsTheFirstConsentDateAndUpdatesUpdatedAt()
    {
        await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Wheelchair));
        _time.Advance(TimeSpan.FromHours(1));

        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Crutches));

        Assert.Equal(Now.UtcDateTime, saved.ConsentGivenAt);
        Assert.Equal(Now.UtcDateTime.AddHours(1), saved.UpdatedAt);
        Assert.Single(_profiles.Profiles);
    }

    [Fact]
    public async Task DeleteProfileAsync_ThenSaveAgain_SetsANewConsentDateAndKeepsTheAccount()
    {
        await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Wheelchair));
        await _service.DeleteProfileAsync(_user.Id);
        Assert.Null(await _service.GetProfileAsync(_user.Id));
        _time.Advance(TimeSpan.FromDays(2));

        var saved = await _service.SaveProfileAsync(_user.Id, AllFields(MobilityProfile.Wheelchair));

        Assert.Equal(Now.UtcDateTime.AddDays(2), saved.ConsentGivenAt);
        Assert.Single(_users.Users);
    }
}
