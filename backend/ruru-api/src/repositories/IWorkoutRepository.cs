using System;
using ruru_api.DTOs.Workouts;
using ruru_api.model;
namespace ruru_api.repositories;


public interface IWorkoutRepository
{
        Task<Workout?> GetByIdAsync(Guid id);
        Task<List<Workout>> GetByUserIdAsync(Guid userId);
        Task<Workout> AddAsync(Guid userId, CreateWorkoutRequest request);
        Task SaveChangesAsync();
}
