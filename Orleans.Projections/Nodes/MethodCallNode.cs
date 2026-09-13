using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record MethodCallNode (INode? Instance, MethodDescriptor Method, List<INode> Parameters) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.Call(Instance?.BuildExpression(context), Method.Resolve(), Parameters.Select(p => p.BuildExpression(context)));
    }

    public virtual bool Equals (MethodCallNode? other) =>
        other is not null
        && (Instance?.Equals(other.Instance) ?? other.Instance is null)
        && Method.Equals(other.Method)
        && Parameters.SequenceEqual(other.Parameters);

    public override int GetHashCode ()
    {
        var hash = new HashCode();
        hash.Add(Instance);
        hash.Add(Method);
        foreach (var parameter in Parameters)
        {
            hash.Add(parameter);
        }
        return hash.ToHashCode();
    }
}