using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

public record MethodCallNode (MethodDescriptor Method, List<INode> Parameters) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Call(Method.Resolve(), Parameters.Select(p => p.BuildExpression(parameter)));
    }
}