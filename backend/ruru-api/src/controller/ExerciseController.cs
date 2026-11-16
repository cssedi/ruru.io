using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
using ruru_api.model;
using ruru_api.services.ExerciseService;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExerciseController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExerciseController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetExercises([FromQuery] EquipmentGroup? equipmentGroup, [FromQuery] string? search)
        {
            var result = await _exerciseService.GetAsync(equipmentGroup, search);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }
    }
}
