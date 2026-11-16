using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ruru_api.ai;

namespace ruru_api.Tests.Unit;

public class AnthropicPlanGeneratorTests
{
    private static readonly CatalogueExercise PushUp = new(Guid.NewGuid(), "Push-up", "Body Only", "Beginner", "Chest", "Strength");
    private static readonly CatalogueExercise Squat = new(Guid.NewGuid(), "Bodyweight squat", "Body Only", "Beginner", "Quadriceps", "Strength");

    [Fact]
    public void ParsePlan_MapsCodesToCatalogueIdsAndDropsUnknownExercises()
    {
        var body = ToolResponse(new JsonArray(
            Exercise("e1", "made-up name"),
            Exercise("e99", "not in the catalogue"),
            Exercise("e2", "Bodyweight squat")));

        var result = AnthropicPlanGenerator.ParsePlan(body, Codes());

        Assert.True(result.Succeeded);
        var day = Assert.Single(result.Value!.Days);
        Assert.Equal(DayOfWeek.Monday, day.Day);
        Assert.Equal(new[] { PushUp.Id, Squat.Id }, day.Exercises.Select(e => e.ExerciseId));
        Assert.Equal("Push-up", day.Exercises[0].Name);
        Assert.Equal("8–10", day.Exercises[0].Reps);
    }

    [Fact]
    public void ParsePlan_WhenCutOff_Fails()
    {
        var body = new JsonObject { ["stop_reason"] = "max_tokens", ["content"] = new JsonArray() }.ToJsonString();

        Assert.False(AnthropicPlanGenerator.ParsePlan(body, Codes()).Succeeded);
    }

    [Theory]
    [InlineData("""{"content":[{"type":"text","text":"Here is a plan"}]}""")]
    [InlineData("not json")]
    public void ParsePlan_WithoutTheToolCall_Fails(string body)
    {
        Assert.False(AnthropicPlanGenerator.ParsePlan(body, Codes()).Succeeded);
    }

    [Fact]
    public async Task Generate_WithoutAnApiKey_FailsWithoutCallingTheApi()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, ToolResponse(new JsonArray()));
        var generator = NewGenerator(handler, apiKey: null);

        var result = await generator.GenerateAsync(Request(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task Generate_SendsTheSkillAsSystemPromptAndForcesTheTool()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, ToolResponse(new JsonArray(Exercise("e1", ""))));
        var generator = NewGenerator(handler, apiKey: "test-key");

        var result = await generator.GenerateAsync(Request(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("test-key", handler.Request!.Headers.GetValues("x-api-key").Single());
        var sent = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("claude-test", sent["model"]!.GetValue<string>());
        Assert.Equal("save_training_plan", sent["tool_choice"]!["name"]!.GetValue<string>());
        var system = sent["system"]!.GetValue<string>();
        Assert.Contains("Program Architect", system);
        Assert.Contains("Exercise Guide", system);
        var user = sent["messages"]![0]!["content"]!.GetValue<string>();
        Assert.Contains("e1 | Push-up", user);
        Assert.Contains("Sensitive knees", user);
    }

    [Fact]
    public async Task Generate_WhenTheApiFails_ReturnsAFriendlyError()
    {
        var generator = NewGenerator(new RecordingHandler(HttpStatusCode.TooManyRequests, "{}"), apiKey: "test-key");

        var result = await generator.GenerateAsync(Request(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("429", Assert.Single(result.Errors));
    }

    private static Dictionary<string, CatalogueExercise> Codes() => new() { ["e1"] = PushUp, ["e2"] = Squat };

    private static PlanRequest Request() => new()
    {
        MainGoal = "Build muscle",
        TrainingLocation = "Bodyweight",
        TrainingDays = [DayOfWeek.Monday],
        WorkoutMinutes = 30,
        HealthConsiderations = ["Sensitive knees"],
        Catalogue = [PushUp, Squat],
    };

    private static AnthropicPlanGenerator NewGenerator(HttpMessageHandler handler, string? apiKey)
    {
        var settings = new Dictionary<string, string?> { ["Anthropic:ApiKey"] = apiKey, ["Anthropic:Model"] = "claude-test" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.test/") };
        return new AnthropicPlanGenerator(http, configuration, NullLogger<AnthropicPlanGenerator>.Instance);
    }

    private static JsonObject Exercise(string code, string name) => new()
    {
        ["exerciseCode"] = code,
        ["name"] = name,
        ["sets"] = 3,
        ["reps"] = "8–10",
        ["rest"] = "90 s",
        ["intensity"] = "RPE 7",
        ["notes"] = "",
        ["easierOption"] = "",
    };

    private static string ToolResponse(JsonArray exercises) => new JsonObject
    {
        ["stop_reason"] = "tool_use",
        ["content"] = new JsonArray(new JsonObject
        {
            ["type"] = "tool_use",
            ["name"] = "save_training_plan",
            ["input"] = new JsonObject
            {
                ["title"] = "Plan",
                ["summary"] = "Summary",
                ["durationWeeks"] = 8,
                ["split"] = "Full body",
                ["phases"] = new JsonArray(),
                ["days"] = new JsonArray(new JsonObject
                {
                    ["day"] = "Monday",
                    ["name"] = "Full body",
                    ["focus"] = "",
                    ["exercises"] = exercises,
                }),
                ["warmUp"] = new JsonArray(),
                ["coolDown"] = new JsonArray(),
                ["progression"] = "",
                ["safetyNotes"] = new JsonArray(),
            },
        }),
    }.ToJsonString();

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
