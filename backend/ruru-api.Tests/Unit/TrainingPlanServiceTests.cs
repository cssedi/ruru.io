using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;
using ruru_api.model;
using ruru_api.services.TrainingPlanService;
using ruru_api.Tests.Fakes;

namespace ruru_api.Tests.Unit;

public class TrainingPlanServiceTests
{
    private readonly FakeProfileRepository _profiles = new();
    private readonly FakeExerciseRepository _exercises = new();
    private readonly FakeTrainingPlanRepository _plans = new();
    private readonly FakePlanGenerator _generator = new();
    private readonly TrainingPlanService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public TrainingPlanServiceTests()
    {
        _service = new TrainingPlanService(_profiles, _exercises, _plans, _generator);
        AddExercise("Push-up", EquipmentGroup.Bodyweight, ExperienceLevel.Beginner);
        AddExercise("Muscle-up", EquipmentGroup.Bodyweight, ExperienceLevel.Advanced);
        AddExercise("Dumbbell row", EquipmentGroup.FreeWeights, ExperienceLevel.Beginner);
        AddExercise("Leg press", EquipmentGroup.Machines, ExperienceLevel.Beginner);
        AddExercise("Band pull-apart", EquipmentGroup.Portable, ExperienceLevel.Beginner);
    }

    [Fact]
    public async Task Readiness_ForNewUser_ListsNeededGapsAndCannotGenerate()
    {
        var result = await _service.GetReadinessAsync(_userId);

        Assert.True(result.Succeeded);
        var readiness = result.Value!;
        Assert.False(readiness.CanGenerate);
        Assert.False(readiness.HasAiConsent);
        Assert.Equal(
            new[] { "mainGoal", "trainingLocation", "trainingDays", "workoutMinutes" },
            readiness.Items.Where(i => i.Needed).Select(i => i.Key));
        Assert.All(readiness.Items, i => Assert.Null(i.Value));
    }

    [Fact]
    public async Task Readiness_WithProfile_ShowsAnswersInPlainWords()
    {
        AddProfile();

        var readiness = (await _service.GetReadinessAsync(_userId)).Value!;

        Assert.True(readiness.CanGenerate);
        Assert.Equal("Build muscle", Value(readiness, "mainGoal"));
        Assert.Equal("Monday, Wednesday, Friday", Value(readiness, "trainingDays"));
        Assert.Equal("45 minutes", Value(readiness, "workoutMinutes"));
        Assert.Equal("Sensitive knees", Value(readiness, "healthConsiderations"));
        Assert.Null(Value(readiness, "sleep"));
    }

    [Fact]
    public async Task GiveAiConsent_WithOutdatedVersion_Fails()
    {
        var result = await _service.GiveAiConsentAsync(_userId, new GiveAiConsentRequest { Version = "old" });

        Assert.False(result.Succeeded);
        Assert.Empty(_profiles.Consents);
    }

    [Fact]
    public async Task GiveAiConsent_RecordsAPurposeSeparateFromTheHealthConsent()
    {
        var result = await GiveAiConsentAsync();

        Assert.True(result.Value!.HasAiConsent);
        Assert.Equal(UserConsent.AiPlanPurpose, Assert.Single(_profiles.Consents).Purpose);
    }

    [Fact]
    public async Task Generate_WithoutAiConsent_FailsWithoutCallingTheAi()
    {
        AddProfile();

        var result = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(_generator.Requests);
    }

    [Fact]
    public async Task Generate_WithNeededAnswersMissing_ListsThem()
    {
        await GiveAiConsentAsync();

        var result = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("training days", Assert.Single(result.Errors));
        Assert.Empty(_generator.Requests);
    }

    [Fact]
    public async Task Generate_StoresADraftOnTheUsersTrainingDaysOnly()
    {
        AddProfile();
        await GiveAiConsentAsync();

        var result = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(TrainingPlanStatus.Draft, result.Value!.Status);
        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            result.Value.Plan.Days.Select(d => d.Day));
        var stored = Assert.Single(_plans.Plans);
        Assert.Equal("fake-model", stored.Model);
    }

    [Fact]
    public async Task Generate_SendsOnlyWhatThePlanNeeds()
    {
        AddProfile();
        await GiveAiConsentAsync();

        await _service.GenerateAsync(_userId, CancellationToken.None);

        var request = Assert.Single(_generator.Requests);
        Assert.Equal("Build muscle", request.MainGoal);
        Assert.Equal(45, request.WorkoutMinutes);
        Assert.Equal(new[] { "Sensitive knees" }, request.HealthConsiderations);
        Assert.InRange(request.Age!.Value, 18, 120);
    }

    [Theory]
    [InlineData(TrainingLocation.Bodyweight, new[] { "Push-up" })]
    [InlineData(TrainingLocation.HomeWithEquipment, new[] { "Band pull-apart", "Dumbbell row", "Push-up" })]
    [InlineData(TrainingLocation.Gym, new[] { "Band pull-apart", "Dumbbell row", "Leg press", "Push-up" })]
    public async Task Generate_OffersOnlyExercisesForTheirEquipmentAndLevel(TrainingLocation location, string[] expected)
    {
        AddProfile(location);
        await GiveAiConsentAsync();

        await _service.GenerateAsync(_userId, CancellationToken.None);

        // Beginners never see the advanced Muscle-up.
        Assert.Equal(expected, Assert.Single(_generator.Requests).Catalogue.Select(e => e.Name));
    }

    [Fact]
    public async Task Generate_AgainReplacesTheDraft()
    {
        AddProfile();
        await GiveAiConsentAsync();

        await _service.GenerateAsync(_userId, CancellationToken.None);
        var second = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.Equal(second.Value!.Id, Assert.Single(_plans.Plans).Id);
    }

    [Fact]
    public async Task Generate_WhenTheAiFails_StoresNothing()
    {
        AddProfile();
        await GiveAiConsentAsync();
        _generator.NextResult = Result<GeneratedPlan>.Failure("AI plans aren't set up yet. Try again later.");

        var result = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(_plans.Plans);
    }

    [Fact]
    public async Task Save_MakesTheDraftCurrentAndArchivesTheOldPlan()
    {
        AddProfile();
        await GiveAiConsentAsync();
        var first = (await _service.GenerateAsync(_userId, CancellationToken.None)).Value!;
        await _service.SaveAsync(_userId, first.Id);
        var second = (await _service.GenerateAsync(_userId, CancellationToken.None)).Value!;

        var result = await _service.SaveAsync(_userId, second.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(second.Id, (await _service.GetCurrentAsync(_userId)).Value!.Id);
        Assert.Equal(TrainingPlanStatus.Archived, _plans.Plans.Single(p => p.Id == first.Id).Status);
    }

    [Fact]
    public async Task Discard_DeletesTheDraft()
    {
        AddProfile();
        await GiveAiConsentAsync();
        var draft = (await _service.GenerateAsync(_userId, CancellationToken.None)).Value!;

        var result = await _service.DiscardAsync(_userId, draft.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(_plans.Plans);
        Assert.Null((await _service.GetCurrentAsync(_userId)).Value);
    }

    [Fact]
    public async Task SaveAndDiscard_CannotTouchAnotherUsersPlan()
    {
        AddProfile();
        await GiveAiConsentAsync();
        var draft = (await _service.GenerateAsync(_userId, CancellationToken.None)).Value!;
        var someoneElse = Guid.NewGuid();

        Assert.False((await _service.SaveAsync(someoneElse, draft.Id)).Succeeded);
        Assert.False((await _service.DiscardAsync(someoneElse, draft.Id)).Succeeded);
        Assert.Single(_plans.Plans);
    }

    [Fact]
    public async Task WithdrawAiConsent_StopsGeneration()
    {
        AddProfile();
        await GiveAiConsentAsync();

        await _service.WithdrawAiConsentAsync(_userId);
        var result = await _service.GenerateAsync(_userId, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    private Task<Result<PlanReadinessResponse>> GiveAiConsentAsync() =>
        _service.GiveAiConsentAsync(_userId, new GiveAiConsentRequest { Version = TrainingPlanService.CurrentAiConsentVersion });

    private void AddProfile(TrainingLocation location = TrainingLocation.Bodyweight) => _profiles.Profiles.Add(new UserProfile
    {
        UserId = _userId,
        Sex = Sex.Female,
        DateOfBirth = new DateOnly(1995, 3, 4),
        MainGoal = MainGoal.BuildMuscle,
        Experience = ExperienceLevel.Beginner,
        TrainingLocation = location,
        TrainingDays = ["Monday", "Wednesday", "Friday"],
        WorkoutMinutes = 45,
        HealthConsiderations = ["SensitiveKnees"],
    });

    private void AddExercise(string name, EquipmentGroup group, ExperienceLevel level) => _exercises.Exercises.Add(new Exercise
    {
        Id = Guid.NewGuid(),
        Name = name,
        Equipment = group.ToString(),
        EquipmentGroup = group,
        Level = level,
        PrimaryMuscleGroup = ["Chest"],
    });

    private static string? Value(PlanReadinessResponse readiness, string key) => readiness.Items.Single(i => i.Key == key).Value;
}
