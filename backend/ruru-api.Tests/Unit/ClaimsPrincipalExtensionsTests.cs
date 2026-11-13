using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ruru_api.common;

namespace ruru_api.Tests.Unit;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReadsTheSubClaim()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }, "Bearer"));

        Assert.Equal(userId, principal.GetUserId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void GetUserId_WithoutAValidId_ThrowsUnauthorized(string? sub)
    {
        var claims = sub is null ? Array.Empty<Claim>() : new[] { new Claim(JwtRegisteredClaimNames.Sub, sub) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }
}
