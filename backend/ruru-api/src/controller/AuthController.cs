using Microsoft.AspNetCore.Mvc;
using ruru_api.common;
using ruru_api.DTOs.Auth;
using ruru_api.services.AuthService;

namespace ruru_api.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequestBody dto)
        {
            var result = await _authService.CreateUserAsync(dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }

        [HttpPost("login")]
        public async Task<IActionResult> SignIn(SignInRequestBody dto)
        {
            var result = await _authService.SignInAsync(dto);

            if (!result.Succeeded)
            {
                return BadRequest(new ErrorResponse(result.Errors));
            }

            return Ok(result.Value);
        }
    }
}
