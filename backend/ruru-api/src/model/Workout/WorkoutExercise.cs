using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;

namespace ruru_api.model;

[PrimaryKey(nameof(ExerciseId), nameof(WorkoutId))]
public class WorkoutExercise
{
    public Guid WorkoutId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Weight { get; set; }
    public int? MinReps { get; set; }
    public int? MaxReps { get; set; }
    public Workout Workout { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;

}