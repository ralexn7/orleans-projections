using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record BinaryExpressionNode (INode Left, INode Right, ExpressionType NodeType, MethodDescriptor? Method = null) : INode
{
	public Expression BuildExpression (ParameterExpression parameter)
	{
		var leftExpression = Left.BuildExpression(parameter);
		var rightExpression = Right.BuildExpression(parameter);
		
		// todo: Check lifting to null later
		return Expression.MakeBinary(NodeType, leftExpression, rightExpression, false, method: Method?.Resolve());
	}
}