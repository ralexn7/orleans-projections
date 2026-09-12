using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ConditionalNode (INode Test, INode True, INode False) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        return Expression.Condition(Test.BuildExpression(parameter), True.BuildExpression(parameter), False.BuildExpression(parameter));
    }
}