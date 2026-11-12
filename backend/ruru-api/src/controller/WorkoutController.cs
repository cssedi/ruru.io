using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
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
            var result = await _workoutService.GetByUserIdAsync(userId);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("CreateWorkout")]
        public async Task<IActionResult> CreateWorkout(CreateWorkoutRequest dto)
        {
            var result = await _workoutService.CreateAsync(dto.UserId, dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }
    }
}
