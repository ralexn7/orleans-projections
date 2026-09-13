using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

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
}