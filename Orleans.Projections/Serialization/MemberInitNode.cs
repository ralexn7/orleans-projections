using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MemberAssignmentNode (MemberDescriptor Member, INode Value);

[GenerateSerializer]
public record MemberInitNode (NewNode NewExpression, List<MemberAssignmentNode> Bindings) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var newExpression = (NewExpression) NewExpression.BuildExpression(context);

        var bindings = Bindings.Select(b =>
            (MemberBinding) Expression.Bind(b.Member.Resolve(), b.Value.BuildExpression(context)));

        return Expression.MemberInit(newExpression, bindings);
    }
}