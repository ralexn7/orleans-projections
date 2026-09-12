using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MemberAssignmentNode (MemberDescriptor Member, INode Value);

// todo: Consider adding a MemberListBindingNode and MemberMemberBindingNode for completeness, but they are less common in projections.
[GenerateSerializer]
public record MemberInitNode (NewNode NewExpression, List<MemberAssignmentNode> Bindings) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        var newExpression = (NewExpression) NewExpression.BuildExpression(parameter);

        var bindings = Bindings.Select(b =>
            (MemberBinding) Expression.Bind(b.Member.Resolve(), b.Value.BuildExpression(parameter)));

        return Expression.MemberInit(newExpression, bindings);
    }
}