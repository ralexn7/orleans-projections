using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ConstNode (object? Value) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Constant(Value, typeof(object));
    }
}

[GenerateSerializer]
public record GenericConstNode<T> (T? Value) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Constant(Value, typeof(T));
    }
}