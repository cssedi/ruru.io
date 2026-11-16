namespace ruru_api.DTOs.TrainingPlans;

public class GiveAiConsentRequest
{
    /// <summary>The AI consent text version the user agreed to (from PlanReadinessResponse.AiConsentVersion).</summary>
    public string Version { get; set; } = null!;
}
