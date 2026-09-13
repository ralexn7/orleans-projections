using System.Linq.Expressions;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public class ExpressionBuilder
{
	public static Expression<Func<TState, object?[]>> Build<TState> (ProjectionRequest request)
	{
		List<Expression> accessors = [];
		var parameter = Expression.Parameter(typeof(TState), "state");
		
		var buildContext = new BuildContext(parameter);
		
		accessors.AddRange(request.Nodes.Select(node => node.BuildExpression(buildContext)));
		
		var arrayExpression = Expression.NewArrayInit(typeof(object), accessors.Select(e => Expression.Convert(e, typeof(object))));
		
		var projectorExpression = Expression.Lambda<Func<TState, object?[]>>(arrayExpression, parameter);

		return projectorExpression;
	}
}