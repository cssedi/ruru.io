using ruru_api.model;
using ruru_api.repositories.TrainingPlanRepository;

namespace ruru_api.Tests.Fakes;

/// <summary>In-memory stand-in for TrainingPlanRepository.</summary>
public class FakeTrainingPlanRepository : ITrainingPlanRepository
{
    public List<TrainingPlan> Plans { get; } = [];

    public Task<TrainingPlan?> GetAsync(Guid id, Guid userId) =>
        Task.FromResult(Plans.FirstOrDefault(p => p.Id == id && p.UserId == userId));

    public Task<List<TrainingPlan>> GetByStatusAsync(Guid userId, TrainingPlanStatus status) =>
        Task.FromResult(Plans.Where(p => p.UserId == userId && p.Status == status).OrderByDescending(p => p.CreatedAt).ToList());

    public Task AddAsync(TrainingPlan plan)
    {
        Plans.Add(plan);
        return Task.CompletedTask;
    }

    public void Remove(TrainingPlan plan) => Plans.Remove(plan);

    public Task SaveChangesAsync() => Task.CompletedTask;
}
