using System.Linq.Expressions;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<ProjectionResult> ProjectAsync (ProjectionRequest<TState> request, CancellationToken cancellationToken);
}

[GenerateSerializer]
public record ProjectionRequest<TState> (INode[] Nodes)
{
	public Expression<Func<TState, object?[]>> BuildExpression ()
	{
		List<Expression> accessors = [];
		var parameter = Expression.Parameter(typeof(TState), "state");
		
		var buildContext = new BuildContext(parameter);
		
		accessors.AddRange(Nodes.Select(node => node.BuildExpression(buildContext)));
		
		var arrayExpression = Expression.NewArrayInit(typeof(object), accessors.Select(e => Expression.Convert(e, typeof(object))));
		
		var projectorExpression = Expression.Lambda<Func<TState, object?[]>>(arrayExpression, parameter);

		return projectorExpression;
	}
}

[GenerateSerializer]
public record ProjectionResult (object?[] Values);

public static class ProjectableGrainExtensions
{
	public static async Task<TProjection> Project<TState, TProjection> (this IProjectableGrain<TState> grain, Expression<Func<TState, TProjection>> projection, CancellationToken cancellationToken = default)
	{
		var request = ProjectionRequestBuilder.Build(projection);
		ProjectionResult result = await grain.ProjectAsync(request, cancellationToken);
		
		// map the result values to the projection type
		
		return projection.Compile().Invoke((TState)Activator.CreateInstance(typeof(TState), result.Values)!);
	}
}
