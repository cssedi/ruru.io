using ruru_api.model;

namespace ruru_api.DTOs.Onboarding;

/// <summary>
/// The questionnaire answers, used both to save (PUT /api/Onboarding) and to read them back.
/// Every field is optional while the user is still answering; the required ones are checked
/// when they finish (POST /api/Onboarding/complete). Enums travel as names, e.g. "BuildMuscle".
/// </summary>
public class OnboardingAnswers
{
    // About you
    public Sex? Sex { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public MainGoal? MainGoal { get; set; }

    // Body
    public decimal? Height { get; set; }
    public decimal? CurrentWeight { get; set; }
    public decimal? GoalWeight { get; set; }
    public GoalPace? Pace { get; set; }
    public List<HealthConsideration> HealthConsiderations { get; set; } = [];

    // Lifestyle
    public ActivityLevel? ActivityLevel { get; set; }
    public ExperienceLevel? Experience { get; set; }
    public SleepDuration? Sleep { get; set; }
    public List<GoalBarrier> Barriers { get; set; } = [];

    // Training
    public TrainingLocation? TrainingLocation { get; set; }
    public List<DayOfWeek> TrainingDays { get; set; } = [];
    public int? WorkoutMinutes { get; set; }
    public List<FocusArea> FocusAreas { get; set; } = [];

    // Nutrition
    public DietType? Diet { get; set; }
    public List<ExcludedFood> ExcludedFoods { get; set; } = [];
}
