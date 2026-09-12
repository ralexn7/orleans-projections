using System.Linq.Expressions;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public class ExpressionBuilder
{
    public static Expression<Func<TState, object?[]>> Build<TState> (ProjectionRequest request)
    {
        List<Expression> accessors = [];
        var parameter = Expression.Parameter(typeof(TState), "state");

        foreach (var projectionMember in request.Nodes)
        {
            if (projectionMember is ConstNode constNode)
            {
                accessors.Add(Expression.Constant(constNode.Value, typeof(object)));
            }
            else if (projectionMember is PropertyNode propertyNode)
            {
                Expression accessor = parameter;
                foreach (var memberPath in propertyNode.Path)
                {
                    accessor = Expression.PropertyOrField(accessor, memberPath);
                }
			    
                accessors.Add(Expression.Convert(accessor, typeof(object)));
            }
        }
	    
        var arrayExpression = Expression.NewArrayInit(typeof(object), accessors);
	    
        var projectorExpression = Expression.Lambda<Func<TState, object?[]>>(arrayExpression, parameter);

        return projectorExpression;
    }
}