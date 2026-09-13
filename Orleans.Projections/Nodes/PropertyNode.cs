using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

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

	public virtual bool Equals (PropertyNode? other) =>
		other is not null && Path.SequenceEqual(other.Path);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		foreach (var segment in Path)
		{
			hash.Add(segment);
		}
		return hash.ToHashCode();
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

	public virtual bool Equals (GenericPropertyNode<T>? other) =>
		other is not null && Path.SequenceEqual(other.Path);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		hash.Add(typeof(T));
		foreach (var segment in Path)
		{
			hash.Add(segment);
		}
		return hash.ToHashCode();
	}
}