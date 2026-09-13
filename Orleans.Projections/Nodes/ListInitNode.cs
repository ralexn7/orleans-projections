using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ListInitNode (NewNode NewExpression, List<ElementInitNode> Initializers) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var newExpression = (NewExpression) NewExpression.BuildExpression(context);

        var initializers = Initializers.Select(init =>
            Expression.ElementInit(init.AddMethod.Resolve(), init.Arguments.Select(a => a.BuildExpression(context))));

        return Expression.ListInit(newExpression, initializers);
    }

    public virtual bool Equals (ListInitNode? other) =>
        other is not null && NewExpression.Equals(other.NewExpression) && Initializers.SequenceEqual(other.Initializers);

    public override int GetHashCode ()
    {
        var hash = new HashCode();
        hash.Add(NewExpression);
        foreach (var initializer in Initializers)
        {
            hash.Add(initializer);
        }
        return hash.ToHashCode();
    }
}