using ruru_api.DTOs.Workouts;
using ruru_api.model;
using ruru_api.services.WorkoutService;
using ruru_api.Tests.Fakes;

namespace ruru_api.Tests.Unit;

public class WorkoutServiceTests
{
    private readonly FakeWorkoutRepository _repository = new();
    private readonly WorkoutService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public WorkoutServiceTests()
    {
        _service = new WorkoutService(_repository);
    }

    [Fact]
    public async Task Create_WithName_SavesAndReturnsWorkout()
    {
        var result = await _service.CreateAsync(_userId, new CreateWorkoutRequest { UserId = _userId, Name = "Push day" });

        Assert.True(result.Succeeded);
        Assert.Equal("Push day", result.Value!.Name);
        Assert.Single(_repository.Workouts);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithBlankName_FailsWithoutSaving(string name)
    {
        var result = await _service.CreateAsync(_userId, new CreateWorkoutRequest { UserId = _userId, Name = name });

        Assert.False(result.Succeeded);
        Assert.Empty(_repository.Workouts);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task GetByUserId_ReturnsOnlyThatUsersWorkouts()
    {
        _repository.Workouts.Add(new Workout { Id = Guid.NewGuid(), Name = "Mine 1", UserId = _userId });
        _repository.Workouts.Add(new Workout { Id = Guid.NewGuid(), Name = "Mine 2", UserId = _userId });
        _repository.Workouts.Add(new Workout { Id = Guid.NewGuid(), Name = "Someone else's", UserId = Guid.NewGuid() });

        var result = await _service.GetByUserIdAsync(_userId);

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "Mine 1", "Mine 2" }, result.Value!.Select(w => w.Name));
    }

    [Fact]
    public async Task GetById_WhenMissing_Fails()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
    }
}
