namespace ruru_api.DTOs.TrainingPlans;

/// <summary>
/// What ruru knows about the user that the plan would use, and what's missing.
/// Nothing here asks the user anything: the app lists the gaps and links each one
/// to its onboarding question (<see cref="ReadinessItem.Key"/> is that question's key).
/// </summary>
public class PlanReadinessResponse
{
    /// <summary>True when every Needed item has an answer and the user may generate.</summary>
    public bool CanGenerate { get; init; }

    /// <summary>True when the user agreed to their details being sent to the AI provider.</summary>
    public bool HasAiConsent { get; init; }
    public string AiConsentVersion { get; init; } = null!;

    /// <summary>The health-profile consent from onboarding; without it no answers are stored.</summary>
    public bool HasProfileConsent { get; init; }

    public List<ReadinessItem> Items { get; init; } = [];
}

public class ReadinessItem
{
    /// <summary>The onboarding question key, e.g. "trainingDays".</summary>
    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;

    /// <summary>The answer as the user would read it, or null when it's missing.</summary>
    public string? Value { get; init; }

    /// <summary>Needed: the plan can't be built without it. Otherwise it makes the plan fit better.</summary>
    public bool Needed { get; init; }
}
