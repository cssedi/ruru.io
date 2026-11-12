using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ruru_api.common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id, from the token's "sub" claim (set by AuthService).
    /// Throws <see cref="UnauthorizedAccessException"/> if it's missing, which GlobalExceptionHandler returns as 401.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("The token doesn't contain a valid user id.");
        }

        return userId;
    }
}
