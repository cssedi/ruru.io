namespace ruru_api.model;

public class Exercise
{
    public Guid Id { get; init; }
    public string? Asset { get; init; }
    public string[] PrimaryMuscleGroup { get; init; } = [];
    public string[]? SecondaryMuscleGroup { get; init; } = [];
    public ICollection<WorkoutExercise> WorkoutExercise { get; init; } = new List<WorkoutExercise>();
}