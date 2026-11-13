using System;

namespace ruru_api.DTOs.Workouts;

public class CreateWorkoutRequest
{
   public Guid UserId { get; set; }
   public string Name {get; set;}

}
