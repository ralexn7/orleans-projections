namespace Orleans.Projections.Test;

// --- Sample state the grain projects over ---

[GenerateSerializer]
public record Address (
    [property: Id(0)] string City,
    [property: Id(1)] string Country);

[GenerateSerializer]
public record PersonState (
    [property: Id(0)] string Name,
    [property: Id(1)] int Age,
    [property: Id(2)] Address Address);

// --- A concrete projectable grain ---

public interface IPersonGrain : IProjectableGrain<PersonState>, IGrainWithStringKey
{
    Task SetStateAsync (PersonState state);
}

public class PersonGrain : Grain, IPersonGrain
{
    private PersonState _state = new("<unset>", 0, new Address("<unset>", "<unset>"));

    public Task SetStateAsync (PersonState state)
    {
        _state = state;
        return Task.CompletedTask;
    }

    public async Task<Projection> Get (ProjectionPlan<PersonState> plan, List<object?> parameters, CancellationToken cancellationToken)
    {
        var projector = plan.BuildLambda();

        return new Projection(projector(_state, parameters.ToArray()));
    }
}