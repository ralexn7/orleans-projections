using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record NewNode (ConstructorDescriptor Constructor, List<INode> Arguments) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.New(Constructor.Resolve(), Arguments.Select(a => a.BuildExpression(context)));
    }
}