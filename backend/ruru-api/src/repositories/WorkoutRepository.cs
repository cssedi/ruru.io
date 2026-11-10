using System;
using Microsoft.EntityFrameworkCore;
using ruru_api.DTOs.Workouts;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.repositories;

public class WorkoutRepository : IWorkoutRepository
{
    private readonly RuruDbContext _context;

    public WorkoutRepository(RuruDbContext context)
    {
        _context = context;
    }

    public async Task<Workout?> GetByIdAsync(Guid id){

        Workout response =  await _context.Workout.FindAsync(id).AsTask();
        //TODO: implement error handling

        return response;
    }
       

    public async Task<List<Workout>> GetByUserIdAsync(Guid userId)
    {
        List<Workout> workouts = await _context.Workout
        .Where(w=> w.UserId == userId)
        .ToListAsync();


        return workouts;
    }

    public async Task<Workout> AddAsync(Guid userId, CreateWorkoutRequest request)
    {
        Workout workout = new Workout 
        {
            Id = new Guid(),
            Name = request.Name,
            Date = new DateTime(),
            UserId = userId
        };

        await _context.Workout
                .AddAsync(workout)
                .AsTask();

        return workout;
    }

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();

}
