namespace ruru_api.DTOs.Onboarding;

public class GiveConsentRequest
{
    /// <summary>The version of the consent text the user agreed to (from OnboardingStatusResponse.ConsentVersion).</summary>
    public string Version { get; set; } = null!;
}
