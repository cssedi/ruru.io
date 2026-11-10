using System;

namespace ruru_api.DTOs.Workouts;

public class WorkoutResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTime date { get; init; }
}
