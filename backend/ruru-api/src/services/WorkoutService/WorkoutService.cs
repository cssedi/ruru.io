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
    public async Task<WorkoutResponse> CreateAsync(Guid userId, CreateWorkoutRequest request)
    {
        var response = await _repository.AddAsync(userId, request);
        WorkoutResponse result = new WorkoutResponse()
        {
            Id = response.Id,
            Name = response.Name,
            date = response.Date
        };

        await _repository.SaveChangesAsync();

        return result;
    }

    public async Task<WorkoutResponse?> GetByIdAsync(Guid Id)
    {
        var response = await _repository.GetByIdAsync(Id);

        if (response == null)
        {
            return null;
        }
        WorkoutResponse result = new WorkoutResponse()
        {
            Id = response!.Id,
            Name = response.Name,
            date = response.Date
        };

        return result;
    }

    public async Task<List<WorkoutResponse>> GetByUserIdAsync(Guid userId)
    {
        List<Workout> response = await _repository.GetByUserIdAsync(userId);
        List<WorkoutResponse> result = [];

        response.ForEach(w =>
            {
                WorkoutResponse value = new WorkoutResponse()
                {
                    Id = w.Id,
                    Name = w.Name,
                    date = w.Date
                };
                result.Append(value);
            });

        return result;
    }
}
