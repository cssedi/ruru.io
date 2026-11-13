using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ruru_api.Tests.Api;

public class OnboardingApiTests : IClassFixture<RuruApiFactory>
{
    private const string ConsentVersion = ruru_api.services.OnboardingService.OnboardingService.CurrentConsentVersion;

    private readonly RuruApiFactory _factory;

    public OnboardingApiTests(RuruApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStatus_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/onboarding");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_WithForgedToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.invalid");

        var response = await client.GetAsync("/api/onboarding");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ForNewUser_IsIncomplete()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var response = await client.GetAsync("/api/onboarding");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("isComplete").GetBoolean());
        Assert.False(body.GetProperty("hasConsent").GetBoolean());
        Assert.Equal(ConsentVersion, body.GetProperty("consentVersion").GetString());
    }

    [Fact]
    public async Task SaveAnswers_WithoutConsent_ReturnsBadRequest()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/onboarding", ValidAnswers());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_ConsentSaveAndComplete()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var consent = await client.PostAsJsonAsync("/api/onboarding/consent", new { version = ConsentVersion });
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        var save = await client.PutAsJsonAsync("/api/onboarding", ValidAnswers());
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var saved = await save.Content.ReadFromJsonAsync<JsonElement>();
        var answers = saved.GetProperty("answers");
        Assert.Equal("NonBinary", answers.GetProperty("sex").GetString());
        Assert.Equal(178m, answers.GetProperty("height").GetDecimal());
        Assert.Equal("Monday", answers.GetProperty("trainingDays")[0].GetString());

        var complete = await client.PostAsync("/api/onboarding/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var status = await client.GetFromJsonAsync<JsonElement>("/api/onboarding");
        Assert.True(status.GetProperty("isComplete").GetBoolean());
    }

    [Fact]
    public async Task SaveAnswers_WithUnknownOption_ReturnsBadRequest()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();
        await client.PostAsJsonAsync("/api/onboarding/consent", new { version = ConsentVersion });

        var response = await client.PutAsJsonAsync("/api/onboarding", new { sex = "Robot" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Skip_MarksCompleteWithoutConsent()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var response = await client.PostAsync("/api/onboarding/skip", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isComplete").GetBoolean());
        Assert.False(body.GetProperty("hasConsent").GetBoolean());
    }

    [Fact]
    public async Task WithdrawConsent_DeletesAnswers()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();
        await client.PostAsJsonAsync("/api/onboarding/consent", new { version = ConsentVersion });
        await client.PutAsJsonAsync("/api/onboarding", ValidAnswers());

        var response = await client.DeleteAsync("/api/onboarding/consent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("hasConsent").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("answers").GetProperty("sex").ValueKind);
    }

    [Fact]
    public async Task UsersOnlySeeTheirOwnAnswers()
    {
        var (first, _) = await _factory.CreateSignedInClientAsync();
        await first.PostAsJsonAsync("/api/onboarding/consent", new { version = ConsentVersion });
        await first.PutAsJsonAsync("/api/onboarding", ValidAnswers());
        var (second, _) = await _factory.CreateSignedInClientAsync();

        var status = await second.GetFromJsonAsync<JsonElement>("/api/onboarding");

        Assert.Equal(JsonValueKind.Null, status.GetProperty("answers").GetProperty("sex").ValueKind);
    }

    private static object ValidAnswers() => new
    {
        sex = "NonBinary",
        dateOfBirth = "1994-06-15",
        mainGoal = "LoseWeight",
        height = 178,
        currentWeight = 92,
        goalWeight = 80,
        pace = (string?)null,
        healthConsiderations = new[] { "SensitiveKnees" },
        activityLevel = "ThreeToFive",
        experience = (string?)null,
        sleep = (string?)null,
        barriers = Array.Empty<string>(),
        trainingLocation = "Gym",
        trainingDays = new[] { "Monday", "Wednesday" },
        workoutMinutes = 45,
        focusAreas = Array.Empty<string>(),
        diet = (string?)null,
        excludedFoods = Array.Empty<string>(),
    };
}
