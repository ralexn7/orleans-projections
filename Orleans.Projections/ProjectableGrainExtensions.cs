using System.Linq.Expressions;

namespace Orleans.Projections;

public static class ProjectableGrainExtensions
{
	public static async Task<TProjection> Project<TState, TProjection> (this IProjectableGrain<TState> grain, Expression<Func<TState, TProjection>> projection, CancellationToken cancellationToken = default)
	{
		var plan = ProjectionPlanFactory.ConvertExpressionToPlan(projection);
		
		Projection result = await grain.GetProjection(plan.Item1, plan.Item2, cancellationToken);
		
		return result.ConvertToInstance<TProjection>();
	}
	
}