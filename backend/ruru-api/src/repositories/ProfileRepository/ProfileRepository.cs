using Microsoft.EntityFrameworkCore;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.repositories.ProfileRepository;

public class ProfileRepository : IProfileRepository
{
    private readonly RuruDbContext _context;

    public ProfileRepository(RuruDbContext context)
    {
        _context = context;
    }

    public Task<UserProfile?> GetProfileAsync(Guid userId) =>
        _context.UserProfile.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddProfileAsync(UserProfile profile)
    {
        await _context.UserProfile.AddAsync(profile);
    }

    public Task<List<UserConsent>> GetActiveConsentsAsync(Guid userId, string purpose) =>
        _context.UserConsent
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.WithdrawnAt == null)
            .OrderByDescending(c => c.GivenAt)
            .ToListAsync();

    public async Task AddConsentAsync(UserConsent consent)
    {
        await _context.UserConsent.AddAsync(consent);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
