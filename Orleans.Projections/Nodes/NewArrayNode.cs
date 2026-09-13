using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record NewArrayNode (string ElementType, List<INode> Items) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var elementType = Type.GetType(ElementType, throwOnError: true)!;

        return Expression.NewArrayInit(elementType, Items.Select(i => i.BuildExpression(context)));
    }

    public virtual bool Equals (NewArrayNode? other) =>
        other is not null && ElementType == other.ElementType && Items.SequenceEqual(other.Items);

    public override int GetHashCode ()
    {
        var hash = new HashCode();
        hash.Add(ElementType);
        foreach (var item in Items)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}