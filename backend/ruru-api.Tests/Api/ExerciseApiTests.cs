using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ruru_api.Tests.Api;

public class ExerciseApiTests : IClassFixture<RuruApiFactory>
{
    private readonly RuruApiFactory _factory;

    public ExerciseApiTests(RuruApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetExercises_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/exercise");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExercises_ReturnsTheCatalogueSeededAtStartup()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var exercises = await client.GetFromJsonAsync<JsonElement>("/api/exercise");

        Assert.Equal(500, exercises.GetArrayLength());
        var first = exercises[0];
        Assert.False(string.IsNullOrEmpty(first.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrEmpty(first.GetProperty("equipmentGroup").GetString()));
    }

    [Fact]
    public async Task GetExercises_FiltersByEquipmentGroup()
    {
        var (client, _) = await _factory.CreateSignedInClientAsync();

        var exercises = await client.GetFromJsonAsync<JsonElement>("/api/exercise?equipmentGroup=Bodyweight&search=push");

        Assert.NotEqual(0, exercises.GetArrayLength());
        foreach (var exercise in exercises.EnumerateArray())
        {
            Assert.Equal("Bodyweight", exercise.GetProperty("equipmentGroup").GetString());
            Assert.Contains("push", exercise.GetProperty("name").GetString()!, StringComparison.OrdinalIgnoreCase);
        }
    }
}
