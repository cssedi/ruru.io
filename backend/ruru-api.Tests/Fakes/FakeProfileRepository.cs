using ruru_api.model;
using ruru_api.repositories.ProfileRepository;

namespace ruru_api.Tests.Fakes;

/// <summary>In-memory stand-in for ProfileRepository, so service tests need no database.</summary>
public class FakeProfileRepository : IProfileRepository
{
    public List<UserProfile> Profiles { get; } = [];
    public List<UserConsent> Consents { get; } = [];
    public int SaveCount { get; private set; }

    public Task<UserProfile?> GetProfileAsync(Guid userId) =>
        Task.FromResult(Profiles.FirstOrDefault(p => p.UserId == userId));

    public Task AddProfileAsync(UserProfile profile)
    {
        Profiles.Add(profile);
        return Task.CompletedTask;
    }

    public Task<List<UserConsent>> GetActiveConsentsAsync(Guid userId, string purpose) =>
        Task.FromResult(Consents
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.WithdrawnAt == null)
            .OrderByDescending(c => c.GivenAt)
            .ToList());

    public Task AddConsentAsync(UserConsent consent)
    {
        Consents.Add(consent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
