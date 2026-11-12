namespace ruru_api.model;

// Answer options for the onboarding questionnaire.
// Stored in the database by name (see RuruDbContext) and sent over the API as strings,
// so renaming a member is a breaking change for both.

public enum Sex
{
    Male,
    Female,
    NonBinary
}

public enum MainGoal
{
    LoseWeight,
    BuildMuscle,
    KeepFit
}

public enum GoalPace
{
    Slow,
    Moderate,
    Fast
}

public enum HealthConsideration
{
    SensitiveBack,
    SensitiveKnees,
    Type1Diabetes,
    Type2Diabetes,
    InjuryRecovery,
    LimitedMobility
}

public enum ActivityLevel
{
    NotMuch,
    OneToTwo,
    ThreeToFive,
    SixPlus
}

public enum ExperienceLevel
{
    Beginner,
    Intermediate,
    Advanced
}

public enum SleepDuration
{
    UnderFive,
    FiveToSix,
    SevenToEight,
    OverEight
}

public enum GoalBarrier
{
    BusySchedule,
    Consistency,
    Snacking,
    NotSureWhatToDo,
    LackOfSupport
}

public enum TrainingLocation
{
    Gym,
    HomeWithEquipment,
    Bodyweight
}

public enum FocusArea
{
    Chest,
    Back,
    Shoulders,
    Arms,
    Core,
    Legs
}

public enum DietType
{
    NoPreference,
    Vegetarian,
    Vegan,
    Pescatarian,
    Keto,
    Mediterranean
}

public enum ExcludedFood
{
    Dairy,
    Gluten,
    Eggs,
    Fish,
    Nuts,
    Pork
}
