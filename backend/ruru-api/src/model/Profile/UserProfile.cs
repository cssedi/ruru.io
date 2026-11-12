namespace ruru_api.model;

/// <summary>
/// A user's onboarding answers. One row per user, created on their first save.
/// Most fields are health information (special personal information under POPIA),
/// so they may only be stored while the user has an active <see cref="UserConsent"/>.
/// </summary>
public class UserProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // About you
    public Sex? Sex { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public MainGoal? MainGoal { get; set; }

    // Body
    public decimal? Height { get; set; }
    public decimal? CurrentWeight { get; set; }
    public decimal? GoalWeight { get; set; }
    public GoalPace? Pace { get; set; }
    public string[] HealthConsiderations { get; set; } = [];

    // Lifestyle
    public ActivityLevel? ActivityLevel { get; set; }
    public ExperienceLevel? Experience { get; set; }
    public SleepDuration? Sleep { get; set; }
    public string[] Barriers { get; set; } = [];

    // Training
    public TrainingLocation? TrainingLocation { get; set; }
    public string[] TrainingDays { get; set; } = [];
    public int? WorkoutMinutes { get; set; }
    public string[] FocusAreas { get; set; } = [];

    // Nutrition
    public DietType? Diet { get; set; }
    public string[] ExcludedFoods { get; set; } = [];

    /// <summary>Set when the user finishes or skips onboarding, so it isn't shown again.</summary>
    public DateTime? OnboardingCompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Removes every answer, e.g. when consent is withdrawn. Keeps the completion date.</summary>
    public void ClearAnswers()
    {
        Sex = null;
        DateOfBirth = null;
        MainGoal = null;
        Height = null;
        CurrentWeight = null;
        GoalWeight = null;
        Pace = null;
        HealthConsiderations = [];
        ActivityLevel = null;
        Experience = null;
        Sleep = null;
        Barriers = [];
        TrainingLocation = null;
        TrainingDays = [];
        WorkoutMinutes = null;
        FocusAreas = [];
        Diet = null;
        ExcludedFoods = [];
    }
}
