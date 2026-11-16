using ruru_api.data.Seed;
using ruru_api.model;
using ruru_api.services.ExerciseService;
using ruru_api.Tests.Fakes;

namespace ruru_api.Tests.Unit;

public class ExerciseServiceTests
{
    private readonly FakeExerciseRepository _repository = new();
    private readonly ExerciseService _service;

    public ExerciseServiceTests()
    {
        _service = new ExerciseService(_repository);
    }

    [Fact]
    public void SeedData_Has500UniqueExercises()
    {
        var rows = ExerciseSeedData.Load();

        Assert.Equal(500, rows.Count);
        Assert.Equal(500, rows.Select(r => r.Name.ToLowerInvariant()).Distinct().Count());
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        Assert.All(rows, r => Assert.InRange(r.Name.Length, 1, 120));
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.BodyPart)));
    }

    [Theory]
    [InlineData(EquipmentGroup.FreeWeights, 190)]
    [InlineData(EquipmentGroup.Bodyweight, 140)]
    [InlineData(EquipmentGroup.Machines, 110)]
    [InlineData(EquipmentGroup.Portable, 60)]
    public void SeedData_FollowsTheAgreedEquipmentSplit(EquipmentGroup group, int expected)
    {
        var rows = ExerciseSeedData.Load();

        Assert.Equal(expected, rows.Count(r => r.EquipmentGroup == group));
    }

    [Fact]
    public async Task Seed_AddsEveryExerciseOnce()
    {
        var first = await _service.SeedAsync();
        var second = await _service.SeedAsync();

        Assert.Equal(500, first.Value);
        Assert.Equal(0, second.Value);
        Assert.Equal(500, _repository.Exercises.Count);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Seed_OnlyAddsWhatIsMissing()
    {
        var existing = ExerciseSeedData.Load()[0];
        _repository.Exercises.Add(new Exercise { Id = Guid.NewGuid(), Name = existing.Name.ToUpperInvariant(), Equipment = existing.Equipment });

        var result = await _service.SeedAsync();

        Assert.Equal(499, result.Value);
        Assert.Equal(500, _repository.Exercises.Count);
    }

    [Fact]
    public async Task Get_FiltersByEquipmentGroupAndSearch()
    {
        await _service.SeedAsync();

        var machines = await _service.GetAsync(EquipmentGroup.Machines, null);
        var presses = await _service.GetAsync(null, "  bench PRESS ");

        Assert.Equal(110, machines.Value!.Count);
        Assert.All(machines.Value, e => Assert.Equal(EquipmentGroup.Machines, e.EquipmentGroup));
        Assert.NotEmpty(presses.Value!);
        Assert.All(presses.Value!, e => Assert.Contains("bench press", e.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_WithUnknownEquipmentGroup_Fails()
    {
        var result = await _service.GetAsync((EquipmentGroup)99, null);

        Assert.False(result.Succeeded);
    }
}
