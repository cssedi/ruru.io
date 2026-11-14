namespace ruru_api.model;

public class Exercise
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public ExerciseType Type { get; init; }

    /// <summary>The specific kit, e.g. "Dumbbell" or "Cable".</summary>
    public string Equipment { get; init; } = null!;
    public EquipmentGroup EquipmentGroup { get; init; }
    public ExperienceLevel Level { get; init; }
    public string? Asset { get; init; }
    public string[] PrimaryMuscleGroup { get; init; } = [];
    public string[]? SecondaryMuscleGroup { get; init; } = [];
    public ICollection<WorkoutExercise> WorkoutExercise { get; init; } = new List<WorkoutExercise>();
}
