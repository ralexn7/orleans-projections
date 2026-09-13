using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MethodCallNode (INode? Instance, MethodDescriptor Method, List<INode> Parameters) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.Call(Instance?.BuildExpression(context), Method.Resolve(), Parameters.Select(p => p.BuildExpression(context)));
    }
}