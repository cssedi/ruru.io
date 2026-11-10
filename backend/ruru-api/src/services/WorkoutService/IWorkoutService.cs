using ruru_api.DTOs.Workouts;

namespace ruru_api.services.WorkoutService;

public interface IWorkoutService
{
    Task <WorkoutResponse> CreateAsync(
        Guid userId,
        CreateWorkoutRequest request
    );
    Task<WorkoutResponse?> GetByIdAsync(Guid Id);
    Task<List<WorkoutResponse>> GetByUserIdAsync(Guid userId);
}
