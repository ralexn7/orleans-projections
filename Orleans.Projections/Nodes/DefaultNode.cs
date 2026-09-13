using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

// todo: pass type definition as a type name instead of a generic parameter. It will simplify client code by reducing reflection but may increase network payload size
[GenerateSerializer]
public record DefaultNode<T> : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.Default(typeof(T));
    }
}