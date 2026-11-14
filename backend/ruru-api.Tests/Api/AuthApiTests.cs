using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ruru_api.Tests.Api;

public class AuthApiTests : IClassFixture<RuruApiFactory>
{
    private readonly RuruApiFactory _factory;

    public AuthApiTests(RuruApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsTokenAndMessageAtTheTopLevel()
    {
        // The web app reads `token` straight off the body. A `{ "value": { ... } }` wrapper
        // made it show "Email or password is incorrect" after a successful registration.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", RuruApiFactory.RegisterBody(NewEmail()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("value", out _));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal("Registration Successful!", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsBadRequestWithMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", RuruApiFactory.RegisterBody(NewEmail(), "test123##", "different1##"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Passwords do not match.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Register_WithAnEmailAlreadyInUse_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", RuruApiFactory.RegisterBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/register", RuruApiFactory.RegisterBody(email));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAtTheTopLevel()
    {
        var (_, email) = await _factory.CreateSignedInClientAsync();
        await _factory.ConfirmEmailAsync(email);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "test123##" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("value", out _));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        var (_, email) = await _factory.CreateSignedInClientAsync();
        await _factory.ConfirmEmailAsync(email);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password1#" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Email or password is incorrect.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_BeforeEmailIsConfirmed_ReturnsBadRequest()
    {
        // RequireConfirmedAccount is on and there's no confirmation flow yet, so this is
        // expected to fail. This test documents that until the flow exists.
        var (_, email) = await _factory.CreateSignedInClientAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "test123##" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@ruru.test";
}
