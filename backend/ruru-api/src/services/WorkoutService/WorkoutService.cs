using ruru_api.common;
using ruru_api.DTOs.Workouts;
using ruru_api.model;
using ruru_api.repositories;

namespace ruru_api.services.WorkoutService;

public class WorkoutService : IWorkoutService
{
    private readonly IWorkoutRepository _repository;

    public WorkoutService(IWorkoutRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WorkoutResponse>> CreateAsync(Guid userId, CreateWorkoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<WorkoutResponse>.Failure("Give the workout a name.");
        }

        var workout = await _repository.AddAsync(userId, request);
        await _repository.SaveChangesAsync();

        return Result<WorkoutResponse>.Success(ToResponse(workout));
    }

    public async Task<Result<WorkoutResponse>> GetByIdAsync(Guid id)
    {
        var workout = await _repository.GetByIdAsync(id);

        if (workout is null)
        {
            return Result<WorkoutResponse>.Failure("We couldn't find that workout.");
        }

        return Result<WorkoutResponse>.Success(ToResponse(workout));
    }

    public async Task<Result<List<WorkoutResponse>>> GetByUserIdAsync(Guid userId)
    {
        var workouts = await _repository.GetByUserIdAsync(userId);

        return Result<List<WorkoutResponse>>.Success(workouts.Select(ToResponse).ToList());
    }

    private static WorkoutResponse ToResponse(Workout workout) => new()
    {
        Id = workout.Id,
        Name = workout.Name,
        date = workout.Date,
    };
}
