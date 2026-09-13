using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ConstNode (int ParameterIndex) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.ArrayIndex(context.Constants, Expression.Constant(ParameterIndex));
	}
}

[GenerateSerializer]
public record GenericConstNode<T> (int ParameterIndex) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.Convert(Expression.ArrayIndex(context.Constants, Expression.Constant(ParameterIndex)), typeof(T));
	}
}