using Microsoft.EntityFrameworkCore;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.repositories.TrainingPlanRepository;

public class TrainingPlanRepository : ITrainingPlanRepository
{
    private readonly RuruDbContext _context;

    public TrainingPlanRepository(RuruDbContext context)
    {
        _context = context;
    }

    public Task<TrainingPlan?> GetAsync(Guid id, Guid userId) =>
        _context.TrainingPlan.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    public Task<List<TrainingPlan>> GetByStatusAsync(Guid userId, TrainingPlanStatus status) =>
        _context.TrainingPlan
            .Where(p => p.UserId == userId && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(TrainingPlan plan)
    {
        await _context.TrainingPlan.AddAsync(plan);
    }

    public void Remove(TrainingPlan plan) => _context.TrainingPlan.Remove(plan);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
