namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<Projection> GetProjection (ProjectionPlan<TState> plan, CancellationToken cancellationToken);
}
