namespace ruru_api.DTOs.Onboarding;

public class OnboardingStatusResponse
{
    /// <summary>True while the user has an active, unwithdrawn consent for their health details.</summary>
    public bool HasConsent { get; init; }

    /// <summary>The consent text version the app should show and send back when the user agrees.</summary>
    public string ConsentVersion { get; init; } = null!;

    /// <summary>True once the user has finished or skipped onboarding; the app doesn't show it again.</summary>
    public bool IsComplete { get; init; }

    public OnboardingAnswers Answers { get; init; } = new();
}
