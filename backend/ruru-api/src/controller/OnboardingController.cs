using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
using ruru_api.DTOs.Onboarding;
using ruru_api.services.OnboardingService;

namespace ruru_api.controller
{
    /// <summary>
    /// The signed-in user's onboarding questionnaire and consent.
    /// The user always comes from the bearer token, never from the request, so a user can only
    /// read or change their own answers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _service;

        public OnboardingController(IOnboardingService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.GetStatusAsync(userId));
        }

        [HttpPut]
        public async Task<IActionResult> SaveAnswers([FromBody] OnboardingAnswers answers)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.SaveAnswersAsync(userId, answers));
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.CompleteAsync(userId));
        }

        [HttpPost("skip")]
        public async Task<IActionResult> Skip()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.SkipAsync(userId));
        }

        [HttpPost("consent")]
        public async Task<IActionResult> GiveConsent([FromBody] GiveConsentRequest request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.GiveConsentAsync(userId, request));
        }

        [HttpDelete("consent")]
        public async Task<IActionResult> WithdrawConsent()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return ToResponse(await _service.WithdrawConsentAsync(userId));
        }

        // AuthService puts the user id in the "sub" claim; MapInboundClaims is off, so it keeps that name.
        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

        private IActionResult ToResponse<T>(Result<T> result) =>
            result.Succeeded
                ? Ok(result.Value)
                : BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors });
    }
}
