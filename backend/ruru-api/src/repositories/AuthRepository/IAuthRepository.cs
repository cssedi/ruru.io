using ruru_api.DTOs.Auth;
using ruru_api.common;
using ruru_api.model;

namespace ruru_api.repositories.AuthRepository;

public interface IAuthRepository
{
    public Task<Result<User>> CreateUser(RegisterUserRequestBody requestBody);
    public Task<Result<User?>> SignIn(SignInRequestBody requestBody);
    public Task<User?> GetUser(string username);
    //public Task<User> GetUser(string username);
    //public Task<User> GetUserByEmail(string email);
    
}