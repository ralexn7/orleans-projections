namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ElementInitNode (MethodDescriptor AddMethod, List<INode> Arguments)
{
	public virtual bool Equals (ElementInitNode? other) =>
		other is not null && AddMethod.Equals(other.AddMethod) && Arguments.SequenceEqual(other.Arguments);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		hash.Add(AddMethod);
		foreach (var argument in Arguments)
		{
			hash.Add(argument);
		}
		return hash.ToHashCode();
	}
}