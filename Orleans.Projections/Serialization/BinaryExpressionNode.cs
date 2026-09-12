using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record BinaryExpressionNode (INode Left, INode Right, ExpressionType NodeType) : INode
{
	public Expression BuildExpression (ParameterExpression parameter)
	{
		var leftExpression = Left.BuildExpression(parameter);
		var rightExpression = Right.BuildExpression(parameter);
		
		return Expression.MakeBinary(NodeType, leftExpression, rightExpression);
	}
}