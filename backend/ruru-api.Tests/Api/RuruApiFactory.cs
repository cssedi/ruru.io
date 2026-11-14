using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ruru_api.data;
using ruru_api.model;

namespace ruru_api.Tests.Api;

/// <summary>
/// Runs the real API (Program.cs) in memory with an in-memory database instead of PostgreSQL.
/// Each factory gets its own database, so test classes don't see each other's data.
/// </summary>
public class RuruApiFactory : WebApplicationFactory<Program>
{
    // Read in Program.cs before the host is built, so these go in as environment variables
    // (the same way a deployment would set them) rather than through ConfigureAppConfiguration.
    static RuruApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=unused-in-tests");
        Environment.SetEnvironmentVariable("Api__Key", "test-signing-key-that-is-long-enough-for-hmac-sha256-0123456789");
        Environment.SetEnvironmentVariable("Api__Authority", "http://ruru.test");
        Environment.SetEnvironmentVariable("Api__Audience", "http://ruru.test");
        Environment.SetEnvironmentVariable("Api__ExpireMinutes", "60");
    }

    private readonly string _databaseName = $"ruru-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<RuruDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RuruDbContext>>();
            services.AddDbContext<RuruDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Registers a new user and returns an HTTP client that sends their token.</summary>
    public async Task<(HttpClient Client, string Email)> CreateSignedInClientAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@ruru.test";
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return (client, email);
    }

    /// <summary>Marks the account's email as confirmed, as the (future) confirmation link would.</summary>
    public async Task ConfirmEmailAsync(string email)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);
        user!.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
    }

    public static object RegisterBody(string email, string password = "test123##", string? confirmPassword = null) => new
    {
        firstName = "Thandi",
        lastName = "Mokoena",
        email,
        password,
        confirmPassword = confirmPassword ?? password,
    };

    public record TokenBody(string Message, string Token);
}
