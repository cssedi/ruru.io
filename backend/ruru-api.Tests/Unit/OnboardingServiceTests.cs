using ruru_api.common;
using ruru_api.DTOs.Onboarding;
using ruru_api.model;
using ruru_api.services.OnboardingService;
using ruru_api.Tests.Fakes;

namespace ruru_api.Tests.Unit;

public class OnboardingServiceTests
{
    private readonly FakeProfileRepository _repository = new();
    private readonly OnboardingService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public OnboardingServiceTests()
    {
        _service = new OnboardingService(_repository);
    }

    [Fact]
    public async Task GetStatus_ForNewUser_IsIncompleteWithoutConsent()
    {
        var result = await _service.GetStatusAsync(_userId);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.HasConsent);
        Assert.False(result.Value.IsComplete);
        Assert.Equal(OnboardingService.CurrentConsentVersion, result.Value.ConsentVersion);
    }

    [Fact]
    public async Task GiveConsent_WithCurrentVersion_RecordsConsent()
    {
        var result = await GiveConsentAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.HasConsent);
        var consent = Assert.Single(_repository.Consents);
        Assert.Equal(_userId, consent.UserId);
        Assert.Equal(UserConsent.HealthProfilePurpose, consent.Purpose);
        Assert.Null(consent.WithdrawnAt);
    }

    [Fact]
    public async Task GiveConsent_Twice_KeepsOneRecord()
    {
        await GiveConsentAsync();
        await GiveConsentAsync();

        Assert.Single(_repository.Consents);
    }

    [Fact]
    public async Task GiveConsent_WithOutdatedVersion_Fails()
    {
        var result = await _service.GiveConsentAsync(_userId, new GiveConsentRequest { Version = "old" });

        Assert.False(result.Succeeded);
        Assert.Empty(_repository.Consents);
    }

    [Fact]
    public async Task SaveAnswers_WithoutConsent_FailsAndStoresNothing()
    {
        var result = await _service.SaveAnswersAsync(_userId, ValidAnswers());

        Assert.False(result.Succeeded);
        Assert.Empty(_repository.Profiles);
    }

    [Fact]
    public async Task SaveAnswers_WithConsent_StoresAnswers()
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.HealthConsiderations = [HealthConsideration.SensitiveKnees, HealthConsideration.SensitiveKnees];
        answers.TrainingDays = [DayOfWeek.Monday, DayOfWeek.Friday];

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.True(result.Succeeded);
        var profile = Assert.Single(_repository.Profiles);
        Assert.Equal(178m, profile.Height);
        Assert.Equal(new[] { "SensitiveKnees" }, profile.HealthConsiderations);
        Assert.Equal(new[] { "Monday", "Friday" }, profile.TrainingDays);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Friday }, result.Value!.Answers.TrainingDays);
    }

    [Fact]
    public async Task SaveAnswers_WhenUnder18_Fails()
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17);

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("18 or older"));
    }

    [Fact]
    public async Task SaveAnswers_OnThe18thBirthday_Succeeds()
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18);

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(99, 70, 70)]
    [InlineData(178, 29, 70)]
    [InlineData(178, 70, 301)]
    public async Task SaveAnswers_WithMeasurementsOutOfRange_Fails(int height, int currentWeight, int goalWeight)
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.Height = height;
        answers.CurrentWeight = currentWeight;
        answers.GoalWeight = goalWeight;

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveAnswers_WithUnknownWorkoutLength_Fails()
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.WorkoutMinutes = 20;

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveAnswers_WithUndefinedEnumValue_Fails()
    {
        await GiveConsentAsync();
        var answers = ValidAnswers();
        answers.Sex = (Sex)99;

        var result = await _service.SaveAnswersAsync(_userId, answers);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Complete_WithMissingRequiredAnswers_ListsThem()
    {
        await GiveConsentAsync();
        await _service.SaveAnswersAsync(_userId, new OnboardingAnswers { Sex = Sex.Female });

        var result = await _service.CompleteAsync(_userId);

        Assert.False(result.Succeeded);
        var message = Assert.Single(result.Errors);
        Assert.Contains("date of birth", message);
        Assert.Contains("activity level", message);
        Assert.DoesNotContain("sex", message);
    }

    [Fact]
    public async Task Complete_WithRequiredAnswers_MarksComplete()
    {
        await GiveConsentAsync();
        await _service.SaveAnswersAsync(_userId, ValidAnswers());

        var result = await _service.CompleteAsync(_userId);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.IsComplete);
        Assert.NotNull(_repository.Profiles.Single().OnboardingCompletedAt);
    }

    [Fact]
    public async Task Complete_WithoutConsent_Fails()
    {
        var result = await _service.CompleteAsync(_userId);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Skip_MarksCompleteWithoutConsentOrAnswers()
    {
        var result = await _service.SkipAsync(_userId);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.IsComplete);
        Assert.False(result.Value.HasConsent);
        Assert.Null(_repository.Profiles.Single().Sex);
    }

    [Fact]
    public async Task WithdrawConsent_ClearsAnswersAndKeepsCompletion()
    {
        await GiveConsentAsync();
        await _service.SaveAnswersAsync(_userId, ValidAnswers());
        await _service.CompleteAsync(_userId);

        var result = await _service.WithdrawConsentAsync(_userId);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.HasConsent);
        Assert.True(result.Value.IsComplete);
        var profile = _repository.Profiles.Single();
        Assert.Null(profile.Sex);
        Assert.Null(profile.Height);
        Assert.Empty(profile.TrainingDays);
        Assert.NotNull(_repository.Consents.Single().WithdrawnAt);
    }

    private Task<Result<OnboardingStatusResponse>> GiveConsentAsync() =>
        _service.GiveConsentAsync(_userId, new GiveConsentRequest { Version = OnboardingService.CurrentConsentVersion });

    private static OnboardingAnswers ValidAnswers() => new()
    {
        Sex = Sex.NonBinary,
        DateOfBirth = new DateOnly(1994, 6, 15),
        MainGoal = MainGoal.LoseWeight,
        Height = 178,
        CurrentWeight = 92,
        GoalWeight = 80,
        ActivityLevel = ActivityLevel.ThreeToFive,
        WorkoutMinutes = 45,
    };
}
