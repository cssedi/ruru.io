using ruru_api.common;
using ruru_api.DTOs.Workouts;

namespace ruru_api.services.WorkoutService;

public interface IWorkoutService
{
    Task<Result<WorkoutResponse>> CreateAsync(Guid userId, CreateWorkoutRequest request);
    Task<Result<WorkoutResponse>> GetByIdAsync(Guid id);
    Task<Result<List<WorkoutResponse>>> GetByUserIdAsync(Guid userId);
}
