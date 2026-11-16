using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;

namespace ruru_api.services.TrainingPlanService;

public interface ITrainingPlanService
{
    /// <summary>What ruru knows that the plan would use, and what's missing. Asks the user nothing.</summary>
    Task<Result<PlanReadinessResponse>> GetReadinessAsync(Guid userId);

    /// <summary>Records consent to send the plan details to the AI provider.</summary>
    Task<Result<PlanReadinessResponse>> GiveAiConsentAsync(Guid userId, GiveAiConsentRequest request);

    Task<Result<PlanReadinessResponse>> WithdrawAiConsentAsync(Guid userId);

    /// <summary>Generates a draft plan. Replaces any earlier draft; the saved plan is untouched.</summary>
    Task<Result<TrainingPlanResponse>> GenerateAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The user's saved plan, or null when they have none.</summary>
    Task<Result<TrainingPlanResponse?>> GetCurrentAsync(Guid userId);

    /// <summary>Makes a draft the user's plan. A previously saved plan is archived.</summary>
    Task<Result<TrainingPlanResponse>> SaveAsync(Guid userId, Guid planId);

    /// <summary>Deletes a draft the user didn't want.</summary>
    Task<Result<bool>> DiscardAsync(Guid userId, Guid planId);
}
