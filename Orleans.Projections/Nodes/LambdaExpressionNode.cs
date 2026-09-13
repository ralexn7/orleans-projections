using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record LambdaExpressionNode (INode Body, IParameterNode[] Parameters) : INode
{
	public Expression BuildExpression (BuildContext context)
	{
		var rootLambdaParams = Parameters
			.Select(p => p.New())
			.ToArray();
		
		using var scope = context.DeclareLambdaScope([..rootLambdaParams.Select((p, i) => new BuildContext.LambdaScopeParameter(i.ToString(), p))]);
		
		return Expression.Lambda(Body.BuildExpression(context), rootLambdaParams);
	}

	public virtual bool Equals (LambdaExpressionNode? other) =>
		other is not null && Body.Equals(other.Body) && Parameters.SequenceEqual(other.Parameters);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		hash.Add(Body);
		foreach (var parameter in Parameters)
		{
			hash.Add(parameter);
		}
		return hash.ToHashCode();
	}
}
