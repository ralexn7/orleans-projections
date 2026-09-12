using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record UnaryExpressionNode (INode Operand, ExpressionType NodeType) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        var operandExpression = Operand.BuildExpression(parameter);
        
        return Expression.MakeUnary(NodeType, operandExpression, null!);
    }
}