using ruru_api.model;

namespace ruru_api.repositories.TrainingPlanRepository;

public interface ITrainingPlanRepository
{
    /// <summary>The plan, only if it belongs to this user.</summary>
    Task<TrainingPlan?> GetAsync(Guid id, Guid userId);
    Task<List<TrainingPlan>> GetByStatusAsync(Guid userId, TrainingPlanStatus status);
    Task AddAsync(TrainingPlan plan);
    void Remove(TrainingPlan plan);
    Task SaveChangesAsync();
}
