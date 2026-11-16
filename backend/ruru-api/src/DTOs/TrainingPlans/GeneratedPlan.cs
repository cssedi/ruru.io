namespace ruru_api.DTOs.TrainingPlans;

/// <summary>
/// The plan the AI returns. Its shape is the tool schema in AnthropicPlanGenerator, so the two change together.
/// One weekly template (Days) repeats across the phases, with the load progressed as Progression describes.
/// </summary>
public class GeneratedPlan
{
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public int DurationWeeks { get; set; }
    public string Split { get; set; } = "";
    public List<PlanPhase> Phases { get; set; } = [];
    public List<PlanDay> Days { get; set; } = [];
    public List<string> WarmUp { get; set; } = [];
    public List<string> CoolDown { get; set; } = [];
    public string Progression { get; set; } = "";

    /// <summary>Precautions, e.g. for health considerations. Shown prominently.</summary>
    public List<string> SafetyNotes { get; set; } = [];
}

public class PlanPhase
{
    public string Name { get; set; } = "";
    public string Weeks { get; set; } = "";
    public string Focus { get; set; } = "";
    public string Intensity { get; set; } = "";
}

public class PlanDay
{
    public DayOfWeek Day { get; set; }
    public string Name { get; set; } = "";
    public string Focus { get; set; } = "";
    public List<PlanExercise> Exercises { get; set; } = [];
}

public class PlanExercise
{
    /// <summary>The catalogue exercise's id. Exercises not in the catalogue are removed before the plan is stored.</summary>
    public Guid ExerciseId { get; set; }
    public string Name { get; set; } = "";
    public int Sets { get; set; }
    public string Reps { get; set; } = "";
    public string Rest { get; set; } = "";
    public string Intensity { get; set; } = "";
    public string Notes { get; set; } = "";

    /// <summary>A gentler option, e.g. for equipment limits or a sensitive joint.</summary>
    public string EasierOption { get; set; } = "";
}
