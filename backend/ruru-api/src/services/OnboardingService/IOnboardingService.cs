using ruru_api.common;
using ruru_api.DTOs.Onboarding;

namespace ruru_api.services.OnboardingService;

public interface IOnboardingService
{
    Task<Result<OnboardingStatusResponse>> GetStatusAsync(Guid userId);

    /// <summary>Records the user's consent to process their health details.</summary>
    Task<Result<OnboardingStatusResponse>> GiveConsentAsync(Guid userId, GiveConsentRequest request);

    /// <summary>Withdraws consent and deletes every stored answer.</summary>
    Task<Result<OnboardingStatusResponse>> WithdrawConsentAsync(Guid userId);

    /// <summary>Saves progress. Replaces all stored answers with <paramref name="answers"/>. Requires consent.</summary>
    Task<Result<OnboardingStatusResponse>> SaveAnswersAsync(Guid userId, OnboardingAnswers answers);

    /// <summary>Checks the required answers are present, then marks onboarding as done.</summary>
    Task<Result<OnboardingStatusResponse>> CompleteAsync(Guid userId);

    /// <summary>Marks onboarding as done without saving any answers ("Not right now").</summary>
    Task<Result<OnboardingStatusResponse>> SkipAsync(Guid userId);
}
