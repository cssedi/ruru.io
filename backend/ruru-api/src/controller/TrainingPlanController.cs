using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;
using ruru_api.services.TrainingPlanService;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingPlanController : ControllerBase
    {
        private readonly ITrainingPlanService _trainingPlanService;

        public TrainingPlanController(ITrainingPlanService trainingPlanService)
        {
            _trainingPlanService = trainingPlanService;
        }

        [HttpGet("readiness")]
        public async Task<IActionResult> GetReadiness()
        {
            var result = await _trainingPlanService.GetReadinessAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("consent")]
        public async Task<IActionResult> GiveAiConsent(GiveAiConsentRequest dto)
        {
            var result = await _trainingPlanService.GiveAiConsentAsync(User.GetUserId(), dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpDelete("consent")]
        public async Task<IActionResult> WithdrawAiConsent()
        {
            var result = await _trainingPlanService.WithdrawAiConsentAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate(CancellationToken cancellationToken)
        {
            var result = await _trainingPlanService.GenerateAsync(User.GetUserId(), cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var result = await _trainingPlanService.GetCurrentAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return result.Value is null ? NoContent() : Ok(result.Value);
        }

        [HttpPost("{id:guid}/save")]
        public async Task<IActionResult> Save(Guid id)
        {
            var result = await _trainingPlanService.SaveAsync(User.GetUserId(), id);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Discard(Guid id)
        {
            var result = await _trainingPlanService.DiscardAsync(User.GetUserId(), id);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return NoContent();
        }
    }
}
