using Microsoft.AspNetCore.Mvc;
using ruru_api.DTOs.Workouts;
using ruru_api.services.WorkoutService;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkoutController : ControllerBase
    {
        private readonly IWorkoutService _workoutService;

        public WorkoutController(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        [HttpGet("GetWorkouts/{userId:guid}")]
        public async Task<IActionResult> GetWorkouts(Guid userId)
        {
            var workouts = await _workoutService.GetByUserIdAsync(userId);

            return Ok(workouts);
        }

        [HttpPost("CreateWorkout")]
        public async Task<IActionResult> CreateWorkout(CreateWorkoutRequest dto)
        {
            var workout = await _workoutService.CreateAsync(dto.UserId, dto);

            return Ok(workout);
        }
    }
}
