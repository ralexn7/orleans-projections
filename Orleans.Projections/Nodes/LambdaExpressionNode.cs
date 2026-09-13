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
}
