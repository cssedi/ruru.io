using ruru_api.DTOs.Auth;
using ruru_api.common;
using ruru_api.model;

namespace ruru_api.services.AuthService;

public interface IAuthService
{
    public Task<Result<UserSignInResponseBody>> CreateUserAsync(RegisterUserRequestBody requestBody);
    public Task<Result<UserSignInResponseBody>> SignInAsync(SignInRequestBody requestBody);
}