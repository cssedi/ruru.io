using ruru_api.ai;
using ruru_api.common;
using ruru_api.DTOs.TrainingPlans;

namespace ruru_api.Tests.Fakes;

/// <summary>
/// Stands in for Claude. Records the request it was given and returns a plan that uses the first
/// catalogue exercises on each of the user's training days, plus one Sunday session they didn't pick.
/// </summary>
public class FakePlanGenerator : IPlanGenerator
{
    public string Model => "fake-model";
    public List<PlanRequest> Requests { get; } = [];
    public Result<GeneratedPlan>? NextResult { get; set; }

    public Task<Result<GeneratedPlan>> GenerateAsync(PlanRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (NextResult is not null) return Task.FromResult(NextResult);

        var exercises = request.Catalogue.Take(3)
            .Select(e => new PlanExercise { ExerciseId = e.Id, Name = e.Name, Sets = 3, Reps = "8–10", Rest = "90 s" })
            .ToList();
        var days = request.TrainingDays
            .Select(d => new PlanDay { Day = d, Name = $"{d} session", Exercises = exercises })
            .Append(new PlanDay { Day = DayOfWeek.Sunday, Name = "Not a training day", Exercises = exercises })
            .ToList();

        return Task.FromResult(Result<GeneratedPlan>.Success(new GeneratedPlan
        {
            Title = "Test plan",
            Summary = "A plan for tests.",
            DurationWeeks = 8,
            Split = "Full body",
            Days = days,
        }));
    }
}
