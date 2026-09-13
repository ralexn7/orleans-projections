using System.Collections.Concurrent;
using System.Linq.Expressions;
using Orleans.Projections.Nodes;

namespace Orleans.Projections;

[GenerateSerializer]
public record ProjectionPlan<TState> (INode[] Nodes)
{
	// Reflection cache to avoid recompiling the same expression multiple times. Since all constants are independent of projection shape, the key is the ProjectionPlan instance itself.
	// Let's keep it unbounded for now; I do not expect a large number of different projection plans to be used in a single grain.
	// Note: usually specific state class is used in the single grain, so maybe it is possible to use the usual dictionary here instead of the concurrent one. But let's keep it concurrent for now, just in case.
	private static readonly ConcurrentDictionary<ProjectionPlan<TState>, Func<TState, object?[], object?[]>> ExpressionCache = new();

	public Func<TState, object?[], object?[]> BuildLambda () =>
		ExpressionCache.GetOrAdd(this, static plan => plan.BuildExpression().Compile());

	public Expression<Func<TState, object?[], object?[]>> BuildExpression ()
	{
		List<Expression> accessors = [];

		var root = Expression.Parameter(typeof(TState), "state");
		var constants = Expression.Parameter(typeof(object[]), "constants");

		var buildContext = new BuildContext(root, constants);

		accessors.AddRange(Nodes.Select(node => node.BuildExpression(buildContext)));

		var arrayExpression = Expression.NewArrayInit(typeof(object), accessors.Select(e => Expression.Convert(e, typeof(object))));

		return Expression.Lambda<Func<TState, object?[], object?[]>>(arrayExpression, root, constants);
	}

	public virtual bool Equals (ProjectionPlan<TState>? other) =>
		other is not null && Nodes.SequenceEqual(other.Nodes);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		foreach (var node in Nodes)
		{
			hash.Add(node);
		}
		return hash.ToHashCode();
	}
}
