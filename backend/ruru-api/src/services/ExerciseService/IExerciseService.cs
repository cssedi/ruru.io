using ruru_api.common;
using ruru_api.DTOs.Exercises;
using ruru_api.model;

namespace ruru_api.services.ExerciseService;

public interface IExerciseService
{
    Task<Result<List<ExerciseResponse>>> GetAsync(EquipmentGroup? equipmentGroup, string? search);

    /// <summary>
    /// Adds any exercise from the starter catalogue that isn't in the database yet (matched by name).
    /// Safe to run on every startup. Returns how many were added.
    /// </summary>
    Task<Result<int>> SeedAsync();
}
