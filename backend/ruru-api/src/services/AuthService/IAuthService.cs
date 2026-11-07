using ruru_api.DTOs.Auth;
using ruru_api.src.common;
using ruru_api.src.model;

namespace ruru_api.services.AuthService;

public interface IAuthService
{
    public Task<Result<UserSignInResponseBody>> CreateUserAsync(RegisterUserRequestBody requestBody);
    public Task<Result<UserSignInResponseBody>> SignInAsync(SignInRequestBody requestBody);
}