using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record NewArrayNode (string ElementType, List<INode> Items) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var elementType = Type.GetType(ElementType, throwOnError: true)!;

        return Expression.NewArrayInit(elementType, Items.Select(i => i.BuildExpression(context)));
    }
}