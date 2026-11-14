using Microsoft.EntityFrameworkCore;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.repositories.ExerciseRepository;

public class ExerciseRepository : IExerciseRepository
{
    private readonly RuruDbContext _context;

    public ExerciseRepository(RuruDbContext context)
    {
        _context = context;
    }

    public Task<List<Exercise>> GetAsync(EquipmentGroup? equipmentGroup, string? search)
    {
        var query = _context.Exercise.AsQueryable();

        if (equipmentGroup is not null)
        {
            var group = equipmentGroup.Value;
            query = query.Where(e => e.EquipmentGroup == group);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term));
        }

        return query.OrderBy(e => e.Name).ToListAsync();
    }

    public Task<List<string>> GetNamesAsync() =>
        _context.Exercise.Select(e => e.Name).ToListAsync();

    public async Task AddRangeAsync(IEnumerable<Exercise> exercises)
    {
        await _context.Exercise.AddRangeAsync(exercises);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
