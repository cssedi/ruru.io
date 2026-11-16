namespace ruru_api.model;

// Stored in the database by name (see RuruDbContext) and sent over the API as strings.

public enum ExerciseType
{
    Strength,
    Stretching,
    Plyometrics,
    Powerlifting,
    Cardio,
    OlympicWeightlifting,
    Strongman
}

/// <summary>What a user needs access to for an exercise; lines up with where they train.</summary>
public enum EquipmentGroup
{
    FreeWeights,
    Bodyweight,
    Machines,
    Portable
}
