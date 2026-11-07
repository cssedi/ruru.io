using ruru_api.DTOs.Auth;
using ruru_api.src.common;
using ruru_api.src.model;

namespace ruru_api.src.repositories.AuthRepository;

public interface IAuthRepository
{
    public Task<Result<User>> CreateUser(RegisterUserRequestBody requestBody);
    public Task<Result<User?>> SignIn(SignInRequestBody requestBody);
    public Task<User?> GetUser(string username);
    //public Task<User> GetUser(string username);
    //public Task<User> GetUserByEmail(string email);
    
}