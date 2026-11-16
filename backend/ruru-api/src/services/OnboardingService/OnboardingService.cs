using ruru_api.common;
using ruru_api.DTOs.Onboarding;
using ruru_api.model;
using ruru_api.repositories.ProfileRepository;

namespace ruru_api.services.OnboardingService;

public class OnboardingService : IOnboardingService
{
    /// <summary>
    /// Version of the consent text shown in the app (frontend: src/onboarding/consent.ts).
    /// Bump both together whenever the wording changes.
    /// </summary>
    public const string CurrentConsentVersion = "2026-11-12";

    public const int MinimumAge = 18;

    private static readonly int[] WorkoutMinuteOptions = [15, 30, 45, 60, 90];

    private readonly IProfileRepository _repository;

    public OnboardingService(IProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<OnboardingStatusResponse>> GetStatusAsync(Guid userId)
    {
        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    public async Task<Result<OnboardingStatusResponse>> GiveConsentAsync(Guid userId, GiveConsentRequest request)
    {
        if (request.Version != CurrentConsentVersion)
        {
            return Result<OnboardingStatusResponse>.Failure("The consent notice has changed. Reload the page and read it again.");
        }

        var active = await _repository.GetActiveConsentsAsync(userId, UserConsent.HealthProfilePurpose);
        if (!active.Any(c => c.Version == request.Version))
        {
            await _repository.AddConsentAsync(new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Purpose = UserConsent.HealthProfilePurpose,
                Version = request.Version,
                GivenAt = DateTime.UtcNow,
            });
            await _repository.SaveChangesAsync();
        }

        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    public async Task<Result<OnboardingStatusResponse>> WithdrawConsentAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        foreach (var consent in await _repository.GetActiveConsentsAsync(userId, UserConsent.HealthProfilePurpose))
        {
            consent.WithdrawnAt = now;
        }

        // Without consent we may not keep the health details (POPIA section 27).
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is not null)
        {
            profile.ClearAnswers();
            profile.UpdatedAt = now;
        }

        await _repository.SaveChangesAsync();
        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    public async Task<Result<OnboardingStatusResponse>> SaveAnswersAsync(Guid userId, OnboardingAnswers answers)
    {
        if (!await HasConsentAsync(userId))
        {
            return Result<OnboardingStatusResponse>.Failure("Agree to the consent notice before saving your details.");
        }

        var errors = Validate(answers);
        if (errors.Count > 0)
        {
            return Result<OnboardingStatusResponse>.Failure(errors);
        }

        var profile = await GetOrAddProfileAsync(userId);
        Apply(answers, profile);
        profile.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    public async Task<Result<OnboardingStatusResponse>> CompleteAsync(Guid userId)
    {
        if (!await HasConsentAsync(userId))
        {
            return Result<OnboardingStatusResponse>.Failure("Agree to the consent notice before finishing.");
        }

        var profile = await _repository.GetProfileAsync(userId);
        var missing = MissingRequiredAnswers(profile);
        if (missing.Count > 0)
        {
            return Result<OnboardingStatusResponse>.Failure($"Still needed: {string.Join(", ", missing)}.");
        }

        profile!.OnboardingCompletedAt ??= DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    public async Task<Result<OnboardingStatusResponse>> SkipAsync(Guid userId)
    {
        var profile = await GetOrAddProfileAsync(userId);
        profile.OnboardingCompletedAt ??= DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return Result<OnboardingStatusResponse>.Success(await BuildStatusAsync(userId));
    }

    private async Task<bool> HasConsentAsync(Guid userId) =>
        (await _repository.GetActiveConsentsAsync(userId, UserConsent.HealthProfilePurpose)).Count > 0;

    private async Task<UserProfile> GetOrAddProfileAsync(Guid userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId, UpdatedAt = DateTime.UtcNow };
            await _repository.AddProfileAsync(profile);
        }
        return profile;
    }

    private async Task<OnboardingStatusResponse> BuildStatusAsync(Guid userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        return new OnboardingStatusResponse
        {
            HasConsent = await HasConsentAsync(userId),
            ConsentVersion = CurrentConsentVersion,
            IsComplete = profile?.OnboardingCompletedAt is not null,
            Answers = profile is null ? new OnboardingAnswers() : ToAnswers(profile),
        };
    }

    private static List<string> Validate(OnboardingAnswers a)
    {
        var errors = new List<string>();

        if (a.DateOfBirth is { } dob)
        {
            var age = AgeOn(dob, DateOnly.FromDateTime(DateTime.UtcNow));
            if (age < MinimumAge) errors.Add($"You need to be {MinimumAge} or older to use ruru.");
            else if (age > 120) errors.Add("Check your date of birth.");
        }

        if (a.Height is < 100m or > 250m) errors.Add("Height must be between 100 and 250 cm.");
        if (a.CurrentWeight is < 30m or > 300m) errors.Add("Current weight must be between 30 and 300 kg.");
        if (a.GoalWeight is < 30m or > 300m) errors.Add("Goal weight must be between 30 and 300 kg.");
        if (a.WorkoutMinutes is { } minutes && !WorkoutMinuteOptions.Contains(minutes))
        {
            errors.Add("Workout length must be 15, 30, 45, 60 or 90 minutes.");
        }

        // JSON numbers can deserialise into enum values that don't exist, so check every choice.
        if (!IsDefined(a.Sex) || !IsDefined(a.MainGoal) || !IsDefined(a.Pace) || !IsDefined(a.ActivityLevel)
            || !IsDefined(a.Experience) || !IsDefined(a.Sleep) || !IsDefined(a.TrainingLocation) || !IsDefined(a.Diet)
            || !AllDefined(a.HealthConsiderations) || !AllDefined(a.Barriers) || !AllDefined(a.TrainingDays)
            || !AllDefined(a.FocusAreas) || !AllDefined(a.ExcludedFoods))
        {
            errors.Add("One of the answers isn't a valid option.");
        }

        return errors;
    }

    private static List<string> MissingRequiredAnswers(UserProfile? p)
    {
        var missing = new List<string>();
        if (p?.Sex is null) missing.Add("sex");
        if (p?.DateOfBirth is null) missing.Add("date of birth");
        if (p?.MainGoal is null) missing.Add("main goal");
        if (p?.Height is null) missing.Add("height");
        if (p?.CurrentWeight is null) missing.Add("current weight");
        if (p?.GoalWeight is null) missing.Add("goal weight");
        if (p?.ActivityLevel is null) missing.Add("activity level");
        return missing;
    }

    private static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    private static void Apply(OnboardingAnswers a, UserProfile p)
    {
        p.Sex = a.Sex;
        p.DateOfBirth = a.DateOfBirth;
        p.MainGoal = a.MainGoal;
        p.Height = a.Height;
        p.CurrentWeight = a.CurrentWeight;
        p.GoalWeight = a.GoalWeight;
        p.Pace = a.Pace;
        p.HealthConsiderations = ToNames(a.HealthConsiderations);
        p.ActivityLevel = a.ActivityLevel;
        p.Experience = a.Experience;
        p.Sleep = a.Sleep;
        p.Barriers = ToNames(a.Barriers);
        p.TrainingLocation = a.TrainingLocation;
        p.TrainingDays = ToNames(a.TrainingDays);
        p.WorkoutMinutes = a.WorkoutMinutes;
        p.FocusAreas = ToNames(a.FocusAreas);
        p.Diet = a.Diet;
        p.ExcludedFoods = ToNames(a.ExcludedFoods);
    }

    private static OnboardingAnswers ToAnswers(UserProfile p) => new()
    {
        Sex = p.Sex,
        DateOfBirth = p.DateOfBirth,
        MainGoal = p.MainGoal,
        Height = p.Height,
        CurrentWeight = p.CurrentWeight,
        GoalWeight = p.GoalWeight,
        Pace = p.Pace,
        HealthConsiderations = FromNames<HealthConsideration>(p.HealthConsiderations),
        ActivityLevel = p.ActivityLevel,
        Experience = p.Experience,
        Sleep = p.Sleep,
        Barriers = FromNames<GoalBarrier>(p.Barriers),
        TrainingLocation = p.TrainingLocation,
        TrainingDays = FromNames<DayOfWeek>(p.TrainingDays),
        WorkoutMinutes = p.WorkoutMinutes,
        FocusAreas = FromNames<FocusArea>(p.FocusAreas),
        Diet = p.Diet,
        ExcludedFoods = FromNames<ExcludedFood>(p.ExcludedFoods),
    };

    // Multi-select answers are stored as text arrays of enum names.
    private static string[] ToNames<T>(IEnumerable<T> values) where T : struct, Enum =>
        values.Distinct().Select(v => v.ToString()).ToArray();

    private static List<T> FromNames<T>(IEnumerable<string> names) where T : struct, Enum
    {
        var result = new List<T>();
        foreach (var name in names)
        {
            if (Enum.TryParse<T>(name, out var value)) result.Add(value);
        }
        return result;
    }

    private static bool IsDefined<T>(T? value) where T : struct, Enum =>
        value is null || Enum.IsDefined(value.Value);

    private static bool AllDefined<T>(IEnumerable<T> values) where T : struct, Enum =>
        values.All(v => Enum.IsDefined(v));
}
