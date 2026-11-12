using ruru_api.model;

namespace ruru_api.repositories.ProfileRepository;

public interface IProfileRepository
{
    Task<UserProfile?> GetProfileAsync(Guid userId);
    Task AddProfileAsync(UserProfile profile);

    /// <summary>Consents for this purpose that haven't been withdrawn, newest first.</summary>
    Task<List<UserConsent>> GetActiveConsentsAsync(Guid userId, string purpose);
    Task AddConsentAsync(UserConsent consent);

    Task SaveChangesAsync();
}
