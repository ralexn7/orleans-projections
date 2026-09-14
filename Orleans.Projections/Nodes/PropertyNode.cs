using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record PropertyNode (string[] Path, INode? Base = null) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		// When no explicit base is provided, member access is rooted at the state parameter.
		// A base node lets the member chain hang off another expression (e.g. an inner lambda parameter).
		Expression accessor = Base?.BuildExpression(context) ?? context.Root;
		foreach (var memberPath in Path)
		{
			accessor = Expression.PropertyOrField(accessor, memberPath);
		}

		return accessor;
	}

	public virtual bool Equals (PropertyNode? other) =>
		other is not null && Path.SequenceEqual(other.Path) && Equals(Base, other.Base);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		foreach (var segment in Path)
		{
			hash.Add(segment);
		}
		hash.Add(Base);
		return hash.ToHashCode();
	}
}

[GenerateSerializer]
public record GenericPropertyNode<T> (string[] Path, INode? Base = null) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		// when no explicit base is provided, member access is rooted at the state parameter.
		Expression accessor = Base?.BuildExpression(context) ?? context.Root;
		foreach (var memberPath in Path)
		{
			accessor = Expression.PropertyOrField(accessor, memberPath);
		}

		return Expression.Convert(accessor, typeof(T));
	}

	public virtual bool Equals (GenericPropertyNode<T>? other) =>
		other is not null && Path.SequenceEqual(other.Path) && Equals(Base, other.Base);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		hash.Add(typeof(T));
		foreach (var segment in Path)
		{
			hash.Add(segment);
		}
		hash.Add(Base);
		return hash.ToHashCode();
	}
}