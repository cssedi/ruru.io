namespace ruru_api.model;

/// <summary>
/// Proof of a user's consent to process their health information (POPIA sections 11 and 27).
/// Rows are never updated except to set <see cref="WithdrawnAt"/>, so the history is kept.
/// </summary>
public class UserConsent
{
    public const string HealthProfilePurpose = "HealthProfile";

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>What the consent covers, e.g. <see cref="HealthProfilePurpose"/>.</summary>
    public string Purpose { get; set; } = HealthProfilePurpose;

    /// <summary>Version of the consent text the user saw, so wording changes can be traced.</summary>
    public string Version { get; set; } = null!;

    public DateTime GivenAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}
