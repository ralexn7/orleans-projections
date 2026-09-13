using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record BinaryExpressionNode (INode Left, INode Right, ExpressionType NodeType, MethodDescriptor? Method = null) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		var leftExpression = Left.BuildExpression(context);
		var rightExpression = Right.BuildExpression(context);
		
		// todo: Check lifting to null later
		return Expression.MakeBinary(NodeType, leftExpression, rightExpression, false, method: Method?.Resolve());
	}
}