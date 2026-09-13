using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ConstNode (int ParameterIndex) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.Constant(context.BuildingParameters[ParameterIndex], typeof(object));
	}
}

[GenerateSerializer]
public record GenericConstNode<T> (int ParameterIndex) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		return Expression.Constant(context.BuildingParameters[ParameterIndex], typeof(T));
	}
}