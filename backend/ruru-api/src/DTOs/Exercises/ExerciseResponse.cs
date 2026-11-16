using ruru_api.model;

namespace ruru_api.DTOs.Exercises;

public class ExerciseResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public ExerciseType Type { get; init; }
    public string Equipment { get; init; } = null!;
    public EquipmentGroup EquipmentGroup { get; init; }
    public ExperienceLevel Level { get; init; }
    public string[] PrimaryMuscleGroup { get; init; } = [];
}
