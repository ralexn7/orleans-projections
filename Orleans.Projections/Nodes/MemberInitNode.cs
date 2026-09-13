using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record MemberAssignmentNode (INode Value);

[GenerateSerializer]
public record MemberInitNode (NewNode NewExpression, List<MemberAssignmentNode> Bindings) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.NewArrayInit(
            typeof(object),
            Bindings.Select(b => Expression.Convert(b.Value.BuildExpression(context), typeof(object))));
    }

    public virtual bool Equals (MemberInitNode? other) =>
        other is not null && NewExpression.Equals(other.NewExpression) && Bindings.SequenceEqual(other.Bindings);

    public override int GetHashCode ()
    {
        var hash = new HashCode();
        hash.Add(NewExpression);
        foreach (var binding in Bindings)
        {
            hash.Add(binding);
        }
        return hash.ToHashCode();
    }
}