using Microsoft.AspNetCore.Identity;
using ruru_api.DTOs.Auth;
using ruru_api.services.AuthService;
using ruru_api.common;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.repositories.AuthRepository;

public class AuthRepository : IAuthRepository
{
    private readonly RuruDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public AuthRepository(RuruDbContext context, UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public async Task<Result<User>> CreateUser(RegisterUserRequestBody requestBody)
    {
        var roleName = nameof(UserRole.User);

        // Check before creating the user: AddToRoleAsync throws (rather than failing) if the role is missing
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return Result<User>.Failure($"Role '{roleName}' does not exist.");
        }

        var user = new User
        {
            UserName = requestBody.Email, // Identity requires a UserName
            Email = requestBody.Email,
            FirstName = requestBody.FirstName,
            LastName = requestBody.LastName,
        };

        // Fails with the password / unique-email rules configured in Program.cs
        var createResult = await _userManager.CreateAsync(user, requestBody.Password);
        if (!createResult.Succeeded)
        {
            return Result<User>.Failure(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            // Don't leave a user in the database without a role
            await _userManager.DeleteAsync(user);
            return Result<User>.Failure(roleResult.Errors.Select(e => e.Description));
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<User?>> SignIn(SignInRequestBody requestBody)
    {
        var userResult = await _userManager.FindByEmailAsync(requestBody.Email);
        
        if (userResult  == null)
        {
            return Result<User?>.Failure("The email address is not registered on our system. Would you like to create an account?");
        }
        
        var signInResult = await _signInManager.CheckPasswordSignInAsync(userResult, requestBody.Password, false);
        if (signInResult.RequiresTwoFactor)
        {
            return Result<User?>.Failure("Please verify your account to continue.");
        }
        if (signInResult.IsLockedOut)
        {
            return Result<User?>.Failure("Your account is locked out.");
        }

        if (signInResult.IsNotAllowed)
        {
            return Result<User?>.Failure("Your account is not allowed to sign in.");
        }

        // A wrong password is none of the cases above, so it has to be checked on its own.
        if (!signInResult.Succeeded)
        {
            return Result<User?>.Failure("Email or password is incorrect.");
        }

        return Result<User?>.Success(userResult);
    }

    public async Task<User?> GetUser(string email)
    {
        var response = await _userManager.FindByEmailAsync(email);
        if (response == null)
        {
            return null;
        }
        return response;
    }
    
    
}