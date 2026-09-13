using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ParameterNode<T> (string? Name) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Parameter(typeof(T), Name);
    }
}