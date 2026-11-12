using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
using ruru_api.DTOs.Onboarding;
using ruru_api.services.OnboardingService;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            var result = await _onboardingService.GetStatusAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> SaveAnswers(OnboardingAnswers dto)
        {
            var result = await _onboardingService.SaveAnswersAsync(User.GetUserId(), dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete()
        {
            var result = await _onboardingService.CompleteAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("skip")]
        public async Task<IActionResult> Skip()
        {
            var result = await _onboardingService.SkipAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("consent")]
        public async Task<IActionResult> GiveConsent(GiveConsentRequest dto)
        {
            var result = await _onboardingService.GiveConsentAsync(User.GetUserId(), dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpDelete("consent")]
        public async Task<IActionResult> WithdrawConsent()
        {
            var result = await _onboardingService.WithdrawConsentAsync(User.GetUserId());

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }
    }
}
