using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ConstNode (object? Value) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.Constant(Value, typeof(object));
	}
}

[GenerateSerializer]
public record GenericConstNode<T> (T? Value) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.Constant(Value, typeof(T));
	}
}