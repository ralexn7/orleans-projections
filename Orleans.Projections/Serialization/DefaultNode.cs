using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

// todo: pass type definition as a type name instead of a generic parameter. It will simplify client code by reducing reflection but may increase network payload size
[GenerateSerializer]
public record DefaultNode<T> : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Default(typeof(T));
    }
}