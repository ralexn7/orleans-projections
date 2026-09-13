using System.Linq.Expressions;
using Orleans.Projections.Nodes;

namespace Orleans.Projections;

[GenerateSerializer]
public record ProjectionPlan<TState> (INode[] Nodes)
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