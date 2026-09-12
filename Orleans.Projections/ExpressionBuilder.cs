using System.Linq.Expressions;

namespace Orleans.Projections;

public class ExpressionBuilder
{
    public static Expression<Func<TState, object?[]>> Build<TState> (ProjectionRequest request)
    {
        List<Expression> accessors = [];
        var parameter = Expression.Parameter(typeof(TState), "state");
        
        accessors.AddRange(request.Nodes.Select(node => node.BuildExpression(parameter)));
	    
        var arrayExpression = Expression.NewArrayInit(typeof(object), accessors);
	    
        var projectorExpression = Expression.Lambda<Func<TState, object?[]>>(arrayExpression, parameter);

        return projectorExpression;
    }
}