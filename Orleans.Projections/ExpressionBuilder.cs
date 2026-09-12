using System.Linq.Expressions;

namespace Orleans.Projections;

public class ExpressionBuilder
{
    public static Expression<Func<TState, object?[]>> Build<TState> (ProjectionRequest request)
    {
        List<Expression> accessors = [];
        var parameter = Expression.Parameter(typeof(TState), "state");

        foreach (var projectionMember in request.Members)
        {
            if (projectionMember is FromConstantMember constantMember)
            {
                accessors.Add(Expression.Constant(constantMember.Value, typeof(object)));
            }
            else if (projectionMember is FromMember fromMember)
            {
                Expression accessor = parameter;
                foreach (var memberPath in fromMember.Paths)
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