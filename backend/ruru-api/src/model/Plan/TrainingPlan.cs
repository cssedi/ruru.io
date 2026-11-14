namespace ruru_api.model;

public enum TrainingPlanStatus
{
    /// <summary>Just generated; the user hasn't saved or discarded it yet.</summary>
    Draft,

    /// <summary>The user's current plan. Only one per user.</summary>
    Saved,

    /// <summary>A plan the user saved before replacing it with a newer one.</summary>
    Archived
}

/// <summary>An AI-generated training plan. The plan itself is stored as JSON in <see cref="Content"/>.</summary>
public class TrainingPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public TrainingPlanStatus Status { get; set; }
    public string Title { get; set; } = null!;

    /// <summary>The plan (<see cref="ruru_api.DTOs.TrainingPlans.GeneratedPlan"/>) serialised as JSON.</summary>
    public string Content { get; set; } = null!;

    /// <summary>The model that wrote the plan, for tracing.</summary>
    public string Model { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime? SavedAt { get; set; }
}
