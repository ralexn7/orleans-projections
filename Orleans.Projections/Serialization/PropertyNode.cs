using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record PropertyNode (string[] Path) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        Expression accessor = parameter;
        foreach (var memberPath in Path)
        {
            accessor = Expression.PropertyOrField(accessor, memberPath);
        }
			    
        return Expression.Convert(accessor, typeof(object));
    }
}