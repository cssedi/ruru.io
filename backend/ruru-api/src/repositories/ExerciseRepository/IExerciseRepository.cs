using ruru_api.model;

namespace ruru_api.repositories.ExerciseRepository;

public interface IExerciseRepository
{
    /// <summary>Exercises ordered by name, optionally narrowed by equipment group and a name search.</summary>
    Task<List<Exercise>> GetAsync(EquipmentGroup? equipmentGroup, string? search);
    Task<List<string>> GetNamesAsync();
    Task AddRangeAsync(IEnumerable<Exercise> exercises);
    Task SaveChangesAsync();
}
