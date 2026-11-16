using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ruru_api.services.OnboardingService;
using ruru_api.services.TrainingPlanService;

namespace ruru_api.Tests.Api;

public class TrainingPlanApiTests : IClassFixture<RuruApiFactory>
{
    private readonly RuruApiFactory _factory;

    public TrainingPlanApiTests(RuruApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Readiness_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/trainingplan/readiness");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_ForNewUser_ShowsWhatsMissing()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var readiness = await client.GetFromJsonAsync<JsonElement>("/api/trainingplan/readiness");

        Assert.False(readiness.GetProperty("canGenerate").GetBoolean());
        var trainingDays = readiness.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("key").GetString() == "trainingDays");
        Assert.True(trainingDays.GetProperty("needed").GetBoolean());
        Assert.Equal(JsonValueKind.Null, trainingDays.GetProperty("value").ValueKind);
    }

    [Fact]
    public async Task Generate_WithoutAiConsent_ReturnsBadRequest()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();
        await AnswerOnboardingAsync(client);

        var response = await client.PostAsync("/api/trainingplan/generate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_GenerateSaveAndReadCurrent()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();
        await AnswerOnboardingAsync(client);

        var noPlan = await client.GetAsync("/api/trainingplan/current");
        Assert.Equal(HttpStatusCode.NoContent, noPlan.StatusCode);

        var consent = await client.PostAsJsonAsync("/api/trainingplan/consent", new { version = TrainingPlanService.CurrentAiConsentVersion });
        Assert.True((await consent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hasAiConsent").GetBoolean());

        var generate = await client.PostAsync("/api/trainingplan/generate", null);
        Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
        var draft = await generate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Draft", draft.GetProperty("status").GetString());
        var days = draft.GetProperty("plan").GetProperty("days");
        Assert.Equal(new[] { "Tuesday", "Thursday" }, days.EnumerateArray().Select(d => d.GetProperty("day").GetString()));

        // The seeded catalogue was narrowed to bodyweight exercises before it reached the AI.
        var request = _factory.PlanGenerator.Requests.Last();
        Assert.NotEmpty(request.Catalogue);
        Assert.All(request.Catalogue, e => Assert.DoesNotContain(e.Equipment, new[] { "Barbell", "Dumbbell", "Machine", "Cable", "Kettlebells" }));
        Assert.All(request.Catalogue, e => Assert.NotEqual("Advanced", e.Level));

        var id = draft.GetProperty("id").GetGuid();
        var save = await client.PostAsync($"/api/trainingplan/{id}/save", null);
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var current = await client.GetFromJsonAsync<JsonElement>("/api/trainingplan/current");
        Assert.Equal(id, current.GetProperty("id").GetGuid());
        Assert.Equal("Saved", current.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Discard_RemovesTheDraft()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();
        await AnswerOnboardingAsync(client);
        await client.PostAsJsonAsync("/api/trainingplan/consent", new { version = TrainingPlanService.CurrentAiConsentVersion });
        var draft = await (await client.PostAsync("/api/trainingplan/generate", null)).Content.ReadFromJsonAsync<JsonElement>();
        var id = draft.GetProperty("id").GetGuid();

        var discard = await client.DeleteAsync($"/api/trainingplan/{id}");

        Assert.Equal(HttpStatusCode.NoContent, discard.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/trainingplan/{id}/save", null)).StatusCode);
    }

    private static async Task AnswerOnboardingAsync(HttpClient client)
    {
        await client.PostAsJsonAsync("/api/onboarding/consent", new { version = OnboardingService.CurrentConsentVersion });
        var save = await client.PutAsJsonAsync("/api/onboarding", new
        {
            sex = "Female",
            dateOfBirth = "1995-03-04",
            mainGoal = "KeepFit",
            height = 165,
            currentWeight = 62,
            goalWeight = 62,
            activityLevel = "OneToTwo",
            trainingLocation = "Bodyweight",
            trainingDays = new[] { "Tuesday", "Thursday" },
            workoutMinutes = 30,
        });
        save.EnsureSuccessStatusCode();
    }
}
