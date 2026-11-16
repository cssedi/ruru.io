using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;

namespace ruru_api.ai;

/// <summary>Writes a training plan. The real one calls Claude; tests use a fake.</summary>
public interface IPlanGenerator
{
    /// <summary>The model name stored with each plan.</summary>
    string Model { get; }

    Task<Result<GeneratedPlan>> GenerateAsync(PlanRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Everything the generator may see. No name, email, user id or date of birth: only what the plan needs.
/// </summary>
public class PlanRequest
{
    public string MainGoal { get; init; } = null!;
    public string TrainingLocation { get; init; } = null!;
    public List<DayOfWeek> TrainingDays { get; init; } = [];
    public int WorkoutMinutes { get; init; }

    public string? Experience { get; init; }
    public string? ActivityLevel { get; init; }
    public string? Pace { get; init; }
    public string? Sex { get; init; }
    public int? Age { get; init; }
    public decimal? Height { get; init; }
    public decimal? CurrentWeight { get; init; }
    public decimal? GoalWeight { get; init; }
    public string? Sleep { get; init; }
    public List<string> HealthConsiderations { get; init; } = [];
    public List<string> FocusAreas { get; init; } = [];

    /// <summary>The exercises the plan may use, already narrowed to the user's equipment and level.</summary>
    public List<CatalogueExercise> Catalogue { get; init; } = [];
}

public record CatalogueExercise(Guid Id, string Name, string Equipment, string Level, string Muscles, string Type);
