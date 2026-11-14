using ruru_api.DTOs.Workouts;
using ruru_api.model;
using ruru_api.repositories;

namespace ruru_api.Tests.Fakes;

/// <summary>In-memory stand-in for WorkoutRepository.</summary>
public class FakeWorkoutRepository : IWorkoutRepository
{
    public List<Workout> Workouts { get; } = [];
    public int SaveCount { get; private set; }

    public Task<Workout?> GetByIdAsync(Guid id) =>
        Task.FromResult(Workouts.FirstOrDefault(w => w.Id == id));

    public Task<List<Workout>> GetByUserIdAsync(Guid userId) =>
        Task.FromResult(Workouts.Where(w => w.UserId == userId).ToList());

    public Task<Workout> AddAsync(Guid userId, CreateWorkoutRequest request)
    {
        var workout = new Workout { Id = Guid.NewGuid(), Name = request.Name, Date = DateTime.UtcNow, UserId = userId };
        Workouts.Add(workout);
        return Task.FromResult(workout);
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
