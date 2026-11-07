using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ruru_api.DTOs.Auth;
using ruru_api.services.AuthService;
using ruru_api.services.WorkoutService;
using ruru_api.src.DTOs.Workouts;

namespace ruru_api.src.controller
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
                 return BadRequest(new { errors = result.Errors });
             }

             return Ok(result.Value);
         }

         [HttpPost("login")]
         public async Task<IActionResult> SignIn(SignInRequestBody dto)
         {
             var response = await _authService.SignInAsync(dto);
             if (!response.Succeeded)
             {
                 return BadRequest(new { response.Errors });
             }
             return Ok(new {response});
         }
     }
}


