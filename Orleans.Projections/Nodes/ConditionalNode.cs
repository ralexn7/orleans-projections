using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ConditionalNode (INode Test, INode True, INode False) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        return Expression.Condition(Test.BuildExpression(context), True.BuildExpression(context), False.BuildExpression(context));
    }
}