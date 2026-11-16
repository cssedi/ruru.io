using ruru_api.model;
using ruru_api.repositories.ExerciseRepository;

namespace ruru_api.Tests.Fakes;

/// <summary>In-memory stand-in for ExerciseRepository.</summary>
public class FakeExerciseRepository : IExerciseRepository
{
    public List<Exercise> Exercises { get; } = [];
    public int SaveCount { get; private set; }

    public Task<List<Exercise>> GetAsync(EquipmentGroup? equipmentGroup, string? search)
    {
        var query = Exercises.AsEnumerable();
        if (equipmentGroup is not null) query = query.Where(e => e.EquipmentGroup == equipmentGroup);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(e => e.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(query.OrderBy(e => e.Name).ToList());
    }

    public Task<List<string>> GetNamesAsync() => Task.FromResult(Exercises.Select(e => e.Name).ToList());

    public Task AddRangeAsync(IEnumerable<Exercise> exercises)
    {
        Exercises.AddRange(exercises);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
