using System;
using System.Runtime.InteropServices;

namespace ruru_api.model;

public class Workout
{
    public Guid Id { get;set; }

    public string Name { get;set; }
    public DateTime Date { get;set; }
    public Guid UserId {get; set;} //User FK
    public User User {get; set;} = null!;
    
    public ICollection<WorkoutExercise> WorkoutExercises {get; init; } = new List<WorkoutExercise>();

}
