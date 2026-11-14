using ruru_api.common;
using ruru_api.data.Seed;
using ruru_api.DTOs.Exercises;
using ruru_api.model;
using ruru_api.repositories.ExerciseRepository;

namespace ruru_api.services.ExerciseService;

public class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _repository;

    public ExerciseService(IExerciseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ExerciseResponse>>> GetAsync(EquipmentGroup? equipmentGroup, string? search)
    {
        if (equipmentGroup is not null && !Enum.IsDefined(equipmentGroup.Value))
        {
            return Result<List<ExerciseResponse>>.Failure("That equipment group doesn't exist.");
        }

        var exercises = await _repository.GetAsync(equipmentGroup, search);

        return Result<List<ExerciseResponse>>.Success(exercises.Select(ToResponse).ToList());
    }

    public async Task<Result<int>> SeedAsync()
    {
        var existing = new HashSet<string>(await _repository.GetNamesAsync(), StringComparer.OrdinalIgnoreCase);

        var missing = ExerciseSeedData.Load()
            .Where(row => !existing.Contains(row.Name))
            .Select(row => new Exercise
            {
                Id = Guid.NewGuid(),
                Name = row.Name,
                Type = row.Type,
                Equipment = row.Equipment,
                EquipmentGroup = row.EquipmentGroup,
                Level = row.Level,
                PrimaryMuscleGroup = [row.BodyPart],
            })
            .ToList();

        if (missing.Count > 0)
        {
            await _repository.AddRangeAsync(missing);
            await _repository.SaveChangesAsync();
        }

        return Result<int>.Success(missing.Count);
    }

    private static ExerciseResponse ToResponse(Exercise exercise) => new()
    {
        Id = exercise.Id,
        Name = exercise.Name,
        Type = exercise.Type,
        Equipment = exercise.Equipment,
        EquipmentGroup = exercise.EquipmentGroup,
        Level = exercise.Level,
        PrimaryMuscleGroup = exercise.PrimaryMuscleGroup,
    };
}
