using ruru_api.model;

namespace ruru_api.DTOs.TrainingPlans;

public class TrainingPlanResponse
{
    public Guid Id { get; init; }
    public TrainingPlanStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public GeneratedPlan Plan { get; init; } = new();
}
