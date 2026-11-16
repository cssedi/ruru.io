using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;

namespace ruru_api.ai;

/// <summary>
/// Writes training plans with Claude through Anthropic's Messages API.
/// The fitness-program skill (program-architect and exercise-guide, in src/ai/skills) is the system prompt,
/// and the plan comes back through a forced tool call so its shape always matches <see cref="GeneratedPlan"/>.
/// Settings: Anthropic:ApiKey (required), Anthropic:Model, Anthropic:MaxTokens.
/// </summary>
public class AnthropicPlanGenerator : IPlanGenerator
{
    public const string DefaultModel = "claude-sonnet-5-5";
    private const string ToolName = "save_training_plan";
    private static readonly string SystemPrompt = BuildSystemPrompt();

    private readonly HttpClient _http;
    private readonly ILogger<AnthropicPlanGenerator> _logger;
    private readonly string? _apiKey;
    private readonly int _maxTokens;

    public AnthropicPlanGenerator(HttpClient http, IConfiguration configuration, ILogger<AnthropicPlanGenerator> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = configuration["Anthropic:ApiKey"];
        Model = configuration["Anthropic:Model"] is { Length: > 0 } model ? model : DefaultModel;
        _maxTokens = int.TryParse(configuration["Anthropic:MaxTokens"], out var max) ? max : 8000;
    }

    public string Model { get; }

    public async Task<Result<GeneratedPlan>> GenerateAsync(PlanRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return Result<GeneratedPlan>.Failure("AI plans aren't set up yet. Try again later.");
        }

        // Short codes keep the prompt small; they're mapped back to catalogue ids below.
        var codes = request.Catalogue.Select((e, i) => (Code: $"e{i + 1}", Exercise: e)).ToList();

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(BuildBody(request, codes).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Add("x-api-key", _apiKey);
        message.Headers.Add("anthropic-version", "2023-06-01");
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The body can echo the request, which holds health details, so only the status is logged.
            _logger.LogWarning("Anthropic API returned {StatusCode} for a plan request", (int)response.StatusCode);
            return Result<GeneratedPlan>.Failure("We couldn't create your plan just now. Try again in a few minutes.");
        }

        return ParsePlan(body, codes.ToDictionary(c => c.Code, c => c.Exercise));
    }

    /// <summary>Reads the tool call out of a Messages API response and maps exercise codes to catalogue ids.</summary>
    public static Result<GeneratedPlan> ParsePlan(string responseBody, IReadOnlyDictionary<string, CatalogueExercise> codes)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(responseBody);
        }
        catch (JsonException)
        {
            return Result<GeneratedPlan>.Failure("The plan came back in a format we couldn't read. Try again.");
        }

        if (root?["stop_reason"]?.GetValue<string>() == "max_tokens")
        {
            return Result<GeneratedPlan>.Failure("The plan was too long to finish. Try again, or pick fewer training days.");
        }

        var input = root?["content"]?.AsArray()
            .FirstOrDefault(block => block?["type"]?.GetValue<string>() == "tool_use" && block["name"]?.GetValue<string>() == ToolName)?["input"];
        if (input is null)
        {
            return Result<GeneratedPlan>.Failure("The plan came back in a format we couldn't read. Try again.");
        }

        GeneratedPlan? plan;
        try
        {
            plan = input.Deserialize<GeneratedPlan>(JsonOptions);
        }
        catch (JsonException)
        {
            return Result<GeneratedPlan>.Failure("The plan came back in a format we couldn't read. Try again.");
        }

        if (plan is null)
        {
            return Result<GeneratedPlan>.Failure("The plan came back empty. Try again.");
        }

        // Map each exercise's code to the catalogue id, using the catalogue's own name.
        var days = input["days"]?.AsArray() ?? [];
        for (var d = 0; d < plan.Days.Count && d < days.Count; d++)
        {
            var exercises = days[d]?["exercises"]?.AsArray() ?? [];
            var kept = new List<PlanExercise>();
            for (var x = 0; x < plan.Days[d].Exercises.Count && x < exercises.Count; x++)
            {
                var code = exercises[x]?["exerciseCode"]?.GetValue<string>();
                if (code is not null && codes.TryGetValue(code, out var match))
                {
                    var exercise = plan.Days[d].Exercises[x];
                    exercise.ExerciseId = match.Id;
                    exercise.Name = match.Name;
                    kept.Add(exercise);
                }
            }
            plan.Days[d].Exercises = kept;
        }

        return Result<GeneratedPlan>.Success(plan);
    }

    private JsonObject BuildBody(PlanRequest request, List<(string Code, CatalogueExercise Exercise)> codes) => new()
    {
        ["model"] = Model,
        ["max_tokens"] = _maxTokens,
        ["system"] = SystemPrompt,
        ["tools"] = new JsonArray(Tool()),
        ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName },
        ["messages"] = new JsonArray(new JsonObject
        {
            ["role"] = "user",
            ["content"] = BuildUserMessage(request, codes),
        }),
    };

    public static string BuildUserMessage(PlanRequest r, IEnumerable<(string Code, CatalogueExercise Exercise)> codes)
    {
        var text = new StringBuilder();
        text.AppendLine("Design a training plan for this person.");
        text.AppendLine();
        text.AppendLine("## Profile");
        Line(text, "Main goal", r.MainGoal);
        Line(text, "Where they train", r.TrainingLocation);
        Line(text, "Training days", string.Join(", ", r.TrainingDays));
        Line(text, "Minutes per session", r.WorkoutMinutes.ToString(CultureInfo.InvariantCulture));
        Line(text, "Training experience", r.Experience ?? "unknown (design for a beginner and say so in the summary)");
        Line(text, "Current activity", r.ActivityLevel);
        Line(text, "Preferred pace", r.Pace);
        Line(text, "Sex", r.Sex);
        Line(text, "Age", r.Age?.ToString(CultureInfo.InvariantCulture));
        Line(text, "Height (cm)", r.Height?.ToString(CultureInfo.InvariantCulture));
        Line(text, "Current weight (kg)", r.CurrentWeight?.ToString(CultureInfo.InvariantCulture));
        Line(text, "Goal weight (kg)", r.GoalWeight?.ToString(CultureInfo.InvariantCulture));
        Line(text, "Usual sleep", r.Sleep);
        Line(text, "Health considerations", r.HealthConsiderations.Count > 0 ? string.Join(", ", r.HealthConsiderations) : "none reported");
        Line(text, "Focus areas", r.FocusAreas.Count > 0 ? string.Join(", ", r.FocusAreas) : "full body");
        text.AppendLine();
        text.AppendLine("## Exercise catalogue");
        text.AppendLine("Use only these exercises, referenced by code. Format: code | name | equipment | level | muscles | type");
        foreach (var (code, e) in codes)
        {
            text.AppendLine($"{code} | {e.Name} | {e.Equipment} | {e.Level} | {e.Muscles} | {e.Type}");
        }
        return text.ToString();
    }

    private static void Line(StringBuilder text, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) text.AppendLine($"- {label}: {value}");
    }

    private static string BuildSystemPrompt()
    {
        var text = new StringBuilder();
        text.AppendLine("""
            You are ruru's training-plan designer. ruru is a South African fitness and nutrition app.
            Follow the two role documents below in order: first design the program as the program architect,
            then add form guidance and easier options as the exercise guide. Ignore their instructions about
            saving files, team communication, nutrition and tracking templates: return the whole plan in one
            call to the save_training_plan tool, and nothing else.

            Rules:
            - Use only exercises from the catalogue in the user's message, referenced by their code.
            - Schedule sessions only on the user's training days, one entry per day, and keep each session
              (warm-up included) within their minutes per session.
            - Plan 4 to 12 weeks with phases, including a deload, and one weekly template that repeats.
            - For every health consideration: avoid or substitute exercises that load that area, give an easier
              option, and add a safety note recommending they check with a doctor or physiotherapist.
            - If experience is unknown, design for a beginner and say so in the summary.
            - Metric units. Plain, friendly South African English. No nutrition, supplement or medical advice.
            """);
        foreach (var name in new[] { "program-architect", "exercise-guide" })
        {
            text.AppendLine();
            text.AppendLine($"# Role document: {name}");
            text.AppendLine(ReadSkill(name));
        }
        return text.ToString();
    }

    private static string ReadSkill(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"skills/fitness-program/{name}.md")
            ?? throw new InvalidOperationException($"The {name} skill isn't embedded in the build.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static JsonObject Tool() => new()
    {
        ["name"] = ToolName,
        ["description"] = "Save the finished training plan for the user.",
        ["input_schema"] = Obj(
            new()
            {
                ["title"] = Str("Short plan name, e.g. '8-week upper/lower strength'."),
                ["summary"] = Str("Two or three sentences: who it's for and how it works."),
                ["durationWeeks"] = new JsonObject { ["type"] = "integer", ["minimum"] = 4, ["maximum"] = 12 },
                ["split"] = Str("e.g. Full body, Upper/Lower, Push/Pull/Legs."),
                ["phases"] = Arr(Obj(new()
                {
                    ["name"] = Str("e.g. Adaptation, Accumulation, Deload."),
                    ["weeks"] = Str("e.g. '1–2'."),
                    ["focus"] = Str(null),
                    ["intensity"] = Str("e.g. 'RPE 6–7' or '65–75% 1RM'."),
                }, "name", "weeks", "focus", "intensity")),
                ["days"] = Arr(Obj(new()
                {
                    ["day"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"),
                    },
                    ["name"] = Str("e.g. 'Push' or 'Full body A'."),
                    ["focus"] = Str(null),
                    ["exercises"] = Arr(Obj(new()
                    {
                        ["exerciseCode"] = Str("The catalogue code, e.g. 'e12'."),
                        ["sets"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 8 },
                        ["reps"] = Str("e.g. '8–10' or '30 s'."),
                        ["rest"] = Str("e.g. '90 s'."),
                        ["intensity"] = Str("e.g. 'RPE 7'."),
                        ["notes"] = Str("One or two form cues, with a warning for risky movements."),
                        ["easierOption"] = Str("A gentler alternative and when to use it."),
                    }, "exerciseCode", "sets", "reps", "rest", "intensity", "notes", "easierOption")),
                }, "day", "name", "focus", "exercises")),
                ["warmUp"] = Arr(Str(null)),
                ["coolDown"] = Arr(Str(null)),
                ["progression"] = Str("How to progress week to week, and when to deload."),
                ["safetyNotes"] = Arr(Str(null)),
            },
            "title", "summary", "durationWeeks", "split", "phases", "days", "warmUp", "coolDown", "progression", "safetyNotes"),
    };

    private static JsonObject Obj(JsonObject properties, params string[] required) => new()
    {
        ["type"] = "object",
        ["properties"] = properties,
        ["required"] = new JsonArray(required.Select(r => (JsonNode)JsonValue.Create(r)!).ToArray()),
    };

    private static JsonObject Arr(JsonObject items) => new() { ["type"] = "array", ["items"] = items };

    private static JsonObject Str(string? description) => description is null
        ? new JsonObject { ["type"] = "string" }
        : new JsonObject { ["type"] = "string", ["description"] = description };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };
}
