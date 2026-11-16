using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ruru_api.ai;
using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;
using ruru_api.model;
using ruru_api.repositories.ExerciseRepository;
using ruru_api.repositories.ProfileRepository;
using ruru_api.repositories.TrainingPlanRepository;

namespace ruru_api.services.TrainingPlanService;

public class TrainingPlanService : ITrainingPlanService
{
    /// <summary>
    /// Version of the AI consent text shown in the app (frontend: src/plans/aiConsent.ts).
    /// Bump both together whenever the wording changes.
    /// </summary>
    public const string CurrentAiConsentVersion = "2026-11-14";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IProfileRepository _profileRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ITrainingPlanRepository _planRepository;
    private readonly IPlanGenerator _generator;

    public TrainingPlanService(
        IProfileRepository profileRepository,
        IExerciseRepository exerciseRepository,
        ITrainingPlanRepository planRepository,
        IPlanGenerator generator)
    {
        _profileRepository = profileRepository;
        _exerciseRepository = exerciseRepository;
        _planRepository = planRepository;
        _generator = generator;
    }

    public async Task<Result<PlanReadinessResponse>> GetReadinessAsync(Guid userId)
    {
        return Result<PlanReadinessResponse>.Success(await BuildReadinessAsync(userId));
    }

    public async Task<Result<PlanReadinessResponse>> GiveAiConsentAsync(Guid userId, GiveAiConsentRequest request)
    {
        if (request.Version != CurrentAiConsentVersion)
        {
            return Result<PlanReadinessResponse>.Failure("The AI notice has changed. Reload the page and read it again.");
        }

        var active = await _profileRepository.GetActiveConsentsAsync(userId, UserConsent.AiPlanPurpose);
        if (!active.Any(c => c.Version == request.Version))
        {
            await _profileRepository.AddConsentAsync(new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Purpose = UserConsent.AiPlanPurpose,
                Version = request.Version,
                GivenAt = DateTime.UtcNow,
            });
            await _profileRepository.SaveChangesAsync();
        }

        return Result<PlanReadinessResponse>.Success(await BuildReadinessAsync(userId));
    }

    public async Task<Result<PlanReadinessResponse>> WithdrawAiConsentAsync(Guid userId)
    {
        foreach (var consent in await _profileRepository.GetActiveConsentsAsync(userId, UserConsent.AiPlanPurpose))
        {
            consent.WithdrawnAt = DateTime.UtcNow;
        }
        await _profileRepository.SaveChangesAsync();

        return Result<PlanReadinessResponse>.Success(await BuildReadinessAsync(userId));
    }

    public async Task<Result<TrainingPlanResponse>> GenerateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var readiness = await BuildReadinessAsync(userId);
        if (!readiness.HasAiConsent)
        {
            return Result<TrainingPlanResponse>.Failure("Agree to the AI notice before creating a plan.");
        }

        if (!readiness.CanGenerate)
        {
            var missing = readiness.Items.Where(i => i.Needed && i.Value is null).Select(i => i.Label.ToLowerInvariant());
            return Result<TrainingPlanResponse>.Failure($"Still needed: {string.Join(", ", missing)}.");
        }

        var profile = (await _profileRepository.GetProfileAsync(userId))!;
        var request = await BuildRequestAsync(profile);
        if (request.Catalogue.Count == 0)
        {
            return Result<TrainingPlanResponse>.Failure("The exercise catalogue is empty, so there's nothing to plan with yet.");
        }

        var generated = await _generator.GenerateAsync(request, cancellationToken);
        if (!generated.Succeeded)
        {
            return Result<TrainingPlanResponse>.Failure(generated.Errors);
        }

        var plan = generated.Value!;

        // Keep only sessions on the user's training days that still have exercises after unknown ones were removed.
        plan.Days = plan.Days
            .Where(d => request.TrainingDays.Contains(d.Day) && d.Exercises.Count > 0)
            .GroupBy(d => d.Day)
            .Select(g => g.First())
            .OrderBy(d => ((int)d.Day + 6) % 7)
            .ToList();
        if (plan.Days.Count == 0)
        {
            return Result<TrainingPlanResponse>.Failure("The plan didn't fit your training days. Try again.");
        }

        // Only one draft at a time: a new one replaces the last.
        foreach (var draft in await _planRepository.GetByStatusAsync(userId, TrainingPlanStatus.Draft))
        {
            _planRepository.Remove(draft);
        }

        var entity = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = TrainingPlanStatus.Draft,
            Title = Truncate(plan.Title, 200),
            Content = JsonSerializer.Serialize(plan, JsonOptions),
            Model = _generator.Model,
            CreatedAt = DateTime.UtcNow,
        };
        await _planRepository.AddAsync(entity);
        await _planRepository.SaveChangesAsync();

        return Result<TrainingPlanResponse>.Success(ToResponse(entity));
    }

    public async Task<Result<TrainingPlanResponse?>> GetCurrentAsync(Guid userId)
    {
        var saved = (await _planRepository.GetByStatusAsync(userId, TrainingPlanStatus.Saved)).FirstOrDefault();

        return Result<TrainingPlanResponse?>.Success(saved is null ? null : ToResponse(saved));
    }

    public async Task<Result<TrainingPlanResponse>> SaveAsync(Guid userId, Guid planId)
    {
        var plan = await _planRepository.GetAsync(planId, userId);
        if (plan is null || plan.Status != TrainingPlanStatus.Draft)
        {
            return Result<TrainingPlanResponse>.Failure("We couldn't find that draft plan. Create a new one.");
        }

        foreach (var previous in await _planRepository.GetByStatusAsync(userId, TrainingPlanStatus.Saved))
        {
            previous.Status = TrainingPlanStatus.Archived;
        }

        plan.Status = TrainingPlanStatus.Saved;
        plan.SavedAt = DateTime.UtcNow;
        await _planRepository.SaveChangesAsync();

        return Result<TrainingPlanResponse>.Success(ToResponse(plan));
    }

    public async Task<Result<bool>> DiscardAsync(Guid userId, Guid planId)
    {
        var plan = await _planRepository.GetAsync(planId, userId);
        if (plan is null || plan.Status != TrainingPlanStatus.Draft)
        {
            return Result<bool>.Failure("We couldn't find that draft plan.");
        }

        _planRepository.Remove(plan);
        await _planRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private async Task<PlanReadinessResponse> BuildReadinessAsync(Guid userId)
    {
        var profile = await _profileRepository.GetProfileAsync(userId);
        var hasProfileConsent = (await _profileRepository.GetActiveConsentsAsync(userId, UserConsent.HealthProfilePurpose)).Count > 0;
        var hasAiConsent = (await _profileRepository.GetActiveConsentsAsync(userId, UserConsent.AiPlanPurpose))
            .Any(c => c.Version == CurrentAiConsentVersion);

        // Keys match the onboarding question keys in the web app, so each gap can link to its question.
        var items = new List<ReadinessItem>
        {
            Item("mainGoal", "Main goal", Words(profile?.MainGoal), needed: true),
            Item("trainingLocation", "Where you train", Words(profile?.TrainingLocation), needed: true),
            Item("trainingDays", "Training days", Join(profile?.TrainingDays), needed: true),
            Item("workoutMinutes", "Workout length", profile?.WorkoutMinutes is { } m ? $"{m} minutes" : null, needed: true),
            Item("experience", "Training experience", Words(profile?.Experience)),
            Item("activityLevel", "How active you are now", Words(profile?.ActivityLevel)),
            Item("focusAreas", "Focus areas", Join(profile?.FocusAreas)),
            Item("healthConsiderations", "Things to work around", HealthValue(profile)),
            Item("pace", "Pace", Words(profile?.Pace)),
            Item("sex", "Sex", Words(profile?.Sex)),
            Item("dateOfBirth", "Age", profile?.DateOfBirth is { } dob ? $"{AgeOn(dob)}" : null),
            Item("height", "Height", profile?.Height is { } h ? $"{h:0.#} cm" : null),
            Item("currentWeight", "Current weight", profile?.CurrentWeight is { } w ? $"{w:0.#} kg" : null),
            Item("goalWeight", "Goal weight", profile?.GoalWeight is { } g ? $"{g:0.#} kg" : null),
            Item("sleep", "Sleep", Words(profile?.Sleep)),
        };

        return new PlanReadinessResponse
        {
            CanGenerate = items.Where(i => i.Needed).All(i => i.Value is not null),
            HasAiConsent = hasAiConsent,
            AiConsentVersion = CurrentAiConsentVersion,
            HasProfileConsent = hasProfileConsent,
            Items = items,
        };
    }

    private async Task<PlanRequest> BuildRequestAsync(UserProfile p)
    {
        EquipmentGroup[] groups = p.TrainingLocation switch
        {
            TrainingLocation.Gym => Enum.GetValues<EquipmentGroup>(),
            TrainingLocation.HomeWithEquipment => [EquipmentGroup.FreeWeights, EquipmentGroup.Bodyweight, EquipmentGroup.Portable],
            _ => [EquipmentGroup.Bodyweight],
        };
        // Beginners and unknowns don't get advanced exercises.
        ExperienceLevel[] levels = p.Experience == ExperienceLevel.Advanced
            ? Enum.GetValues<ExperienceLevel>()
            : [ExperienceLevel.Beginner, ExperienceLevel.Intermediate];

        var catalogue = new List<CatalogueExercise>();
        foreach (var group in groups)
        {
            catalogue.AddRange((await _exerciseRepository.GetAsync(group, null))
                .Where(e => levels.Contains(e.Level))
                .Select(e => new CatalogueExercise(e.Id, e.Name, e.Equipment, e.Level.ToString(), string.Join("/", e.PrimaryMuscleGroup), e.Type.ToString())));
        }

        return new PlanRequest
        {
            MainGoal = Words(p.MainGoal)!,
            TrainingLocation = Words(p.TrainingLocation)!,
            TrainingDays = FromNames<DayOfWeek>(p.TrainingDays),
            WorkoutMinutes = p.WorkoutMinutes!.Value,
            Experience = Words(p.Experience),
            ActivityLevel = Words(p.ActivityLevel),
            Pace = Words(p.Pace),
            Sex = Words(p.Sex),
            Age = p.DateOfBirth is { } dob ? AgeOn(dob) : null,
            Height = p.Height,
            CurrentWeight = p.CurrentWeight,
            GoalWeight = p.GoalWeight,
            Sleep = Words(p.Sleep),
            HealthConsiderations = p.HealthConsiderations.Select(SplitWords).ToList(),
            FocusAreas = p.FocusAreas.ToList(),
            Catalogue = catalogue.OrderBy(e => e.Name).ToList(),
        };
    }

    private static TrainingPlanResponse ToResponse(TrainingPlan plan) => new()
    {
        Id = plan.Id,
        Status = plan.Status,
        CreatedAt = plan.CreatedAt,
        Plan = JsonSerializer.Deserialize<GeneratedPlan>(plan.Content, JsonOptions) ?? new GeneratedPlan(),
    };

    private static ReadinessItem Item(string key, string label, string? value, bool needed = false) =>
        new() { Key = key, Label = label, Value = value, Needed = needed };

    /// <summary>
    /// Onboarding stores "None of these" as an empty list, the same as never answering. Someone who finished
    /// the questions (Sex is required, so it's set) is treated as having answered "None".
    /// </summary>
    private static string? HealthValue(UserProfile? p)
    {
        if (p is null) return null;
        if (p.HealthConsiderations.Length > 0) return Join(p.HealthConsiderations);
        return p.OnboardingCompletedAt is not null && p.Sex is not null ? "None" : null;
    }

    private static string? Words<T>(T? value) where T : struct, Enum => value is null ? null : SplitWords(value.Value.ToString());

    private static string? Join(IEnumerable<string>? names) =>
        names is null || !names.Any() ? null : string.Join(", ", names.Select(SplitWords));

    /// <summary>"ThreeToFive" becomes "Three to five".</summary>
    private static string SplitWords(string name)
    {
        var words = Regex.Replace(name, "(?<=[a-z])(?=[A-Z0-9])", " ").ToLowerInvariant();
        return words.Length == 0 ? name : char.ToUpperInvariant(words[0]) + words[1..];
    }

    private static List<T> FromNames<T>(IEnumerable<string> names) where T : struct, Enum =>
        names.Select(n => Enum.TryParse<T>(n, out var v) ? (T?)v : null).OfType<T>().ToList();

    private static int AgeOn(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrWhiteSpace(value) ? "Your training plan" : value.Length <= max ? value : value[..max];
}
