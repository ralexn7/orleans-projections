using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MethodCallNode (INode? Instance, MethodDescriptor Method, List<INode> Parameters) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Call(Instance?.BuildExpression(parameter), Method.Resolve(), Parameters.Select(p => p.BuildExpression(parameter)));
    }
}