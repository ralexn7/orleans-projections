using System.Linq.Expressions;

namespace Orleans.Projections;

public static class ProjectableGrainExtensions
{
	public static async Task<TProjection> Project<TState, TProjection> (this IProjectableGrain<TState> grain, Expression<Func<TState, TProjection>> projection, CancellationToken cancellationToken = default)
	{
		var request = ProjectionRequestBuilder.Build(projection);
		
		Projection result = await grain.GetProjection(request, cancellationToken);
		
		return result.ToProjection<TProjection>();
	}
	
}