using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ruru_api.services.WorkoutService;
using ruru_api.DTOs.Workouts;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkoutController : ControllerBase
    {
        // Temporary until auth provides the current user.
        private static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

        private readonly IWorkoutService _service;

        public WorkoutController(IWorkoutService service)
        {
            _service = service;
        }

        [HttpGet("GetWorkouts/{userId:guid}")]
        public async Task<ActionResult<List<WorkoutResponse>>> GetWorkouts(Guid userId)
        {
            return await _service.GetByUserIdAsync(userId);
        }

        [HttpPost("CreateWorkout")]
        public async Task<ActionResult<WorkoutResponse>> CreateWorkout([FromBody] CreateWorkoutRequest request)
        {
            return await _service.CreateAsync(TestUserId, request);
        }
    }
}
