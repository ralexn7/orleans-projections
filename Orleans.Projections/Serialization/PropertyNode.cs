using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record PropertyNode (string[] Path) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		Expression accessor = context.Root;
		foreach (var memberPath in Path)
		{
			accessor = Expression.PropertyOrField(accessor, memberPath);
		}

		return accessor;
	}
}

[GenerateSerializer]
public record GenericPropertyNode<T> (string[] Path) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		Expression accessor = context.Root;
		foreach (var memberPath in Path)
		{
			accessor = Expression.PropertyOrField(accessor, memberPath);
		}

		return Expression.Convert(accessor, typeof(T));
	}
}