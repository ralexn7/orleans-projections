using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record UnaryExpressionNode (INode Operand, ExpressionType NodeType) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var operandExpression = Operand.BuildExpression(context);
        
        return Expression.MakeUnary(NodeType, operandExpression, null!);
    }
}

[GenerateSerializer]
public record GenericUnaryExpressionNode<T> (INode Operand, ExpressionType NodeType) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var operandExpression = Operand.BuildExpression(context);
        
        return Expression.MakeUnary(NodeType, operandExpression, typeof(T));
    }
}