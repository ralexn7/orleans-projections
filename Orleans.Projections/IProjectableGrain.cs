using System.Linq.Expressions;

namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<Projection> Get (ProjectionPlan<TState> plan, List<object?> parameters, CancellationToken cancellationToken);
}

public static class ProjectableGrainExtensions
{
	public static async Task<TProjection> Get<TState, TProjection> (this IProjectableGrain<TState> grain, Expression<Func<TState, TProjection>> projection, CancellationToken cancellationToken = default)
	{
		var plan = ProjectionPlanFactory.ConvertExpressionToPlan(projection);
		
		Projection result = await grain.Get(plan.Item1, plan.Item2, cancellationToken);
		
		return result.ConvertToInstance<TProjection>();
	}
}