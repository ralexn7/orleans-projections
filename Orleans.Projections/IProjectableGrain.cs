namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<Projection> GetProjection (ProjectionPlan<TState> plan, List<object?> parameters, CancellationToken cancellationToken);
}
