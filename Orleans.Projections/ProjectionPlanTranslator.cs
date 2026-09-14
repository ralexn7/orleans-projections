using System.Linq.Expressions;
using Orleans.Projections.Nodes;

namespace Orleans.Projections;

public class ProjectionPlanTranslator
{
	public static (ProjectionPlan<TState>, List<object?>) Translate<TState, TProjection> (Expression<Func<TState, TProjection>> projectionExpression)
	{
		var nodes = new List<INode>();
		List<object?> planParameters = [];
		
		IReadOnlyCollection<Expression> arguments = [];
		
		// if it is complex projection, we need to extract the arguments from the NewExpression or MemberInitExpression
		if (projectionExpression.Body is NewExpression newExpression)
		{
			arguments = newExpression.Arguments;
		}
		else if (projectionExpression.Body is MemberInitExpression memberInitExpression)
		{
			// todo: consider adding support for MemberListBinding and MemberMemberBinding
			if (memberInitExpression.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
			{
				throw new NotSupportedException($"Unsupported member binding type: {memberInitExpression.Bindings.First().BindingType}");
			}
			
			arguments = [..memberInitExpression.Bindings.Select(b => ((MemberAssignment)b).Expression)];
		}

		// for scalar projection, we just need to build the node for the body of the expression
		if (arguments is { Count: > 0 })
		{
			nodes.AddRange(arguments.Select(arg => BuildNode(arg, planParameters, projectionExpression.Parameters)));
		}
		else
		{
			nodes.Add(BuildNode(projectionExpression.Body, planParameters, projectionExpression.Parameters));
		}
		
		return (new ProjectionPlan<TState>([..nodes]), planParameters);
	}
	
	private static INode BuildNode (Expression expression, List<object?> planParameters, IReadOnlyCollection<ParameterExpression> rootParameters)
	{
		switch (expression)
		{
			case MemberExpression memberExpression:
				var path = new List<string>();
				Expression? current = memberExpression;

				while (current is MemberExpression currentMember)
				{
					path.Insert(0, currentMember.Member.Name);
					current = currentMember.Expression;
				}

				// determine the base node for the member access. If the base is a root parameter, we don't need to build a node for it.
				var baseIsRoot = current is null || (current is ParameterExpression rootParam && rootParameters.Contains(rootParam));
				var baseNode = baseIsRoot ? null : BuildNode(current!, planParameters, rootParameters);

				// todo: consider not using reflection here and send type as a string instead.
				return (typeof(GenericPropertyNode<>).MakeGenericType(memberExpression.Type).GetConstructor([typeof(string[]), typeof(INode)])!.Invoke([path.ToArray(), baseNode]) as INode)!;

			case ConstantExpression constantExpression:
				// if it is enum, we need to persist its integer value instead of the enum type itself, because the enum type may not be available on the server.
				planParameters.Add(constantExpression.Type.IsEnum
					? Convert.ChangeType(constantExpression.Value, Enum.GetUnderlyingType(constantExpression.Type))
					: constantExpression.Value);
				return (typeof(GenericConstNode<>).MakeGenericType(constantExpression.Type).GetConstructor([typeof(int)])!.Invoke([planParameters.Count - 1]) as INode)!;

			case UnaryExpression unaryExpression:
				return (typeof(GenericUnaryExpressionNode<>).MakeGenericType(unaryExpression.Type).GetConstructor([
					typeof(INode), typeof(ExpressionType)])!.Invoke([BuildNode(unaryExpression.Operand, planParameters, rootParameters), unaryExpression.NodeType]) as INode)!;

			case BinaryExpression binaryExpression:
				return new BinaryExpressionNode(BuildNode(binaryExpression.Left, planParameters, rootParameters), BuildNode(binaryExpression.Right, planParameters, rootParameters), binaryExpression.NodeType);

			case ConditionalExpression conditionalExpression:
				return new ConditionalNode(BuildNode(conditionalExpression.Test, planParameters, rootParameters), BuildNode(conditionalExpression.IfTrue, planParameters, rootParameters), BuildNode(conditionalExpression.IfFalse, planParameters, rootParameters));

			case MethodCallExpression methodCallExpression:
				var methodDescriptor = MethodDescriptor.Create(methodCallExpression.Method);
				var parameters = methodCallExpression.Arguments.Select(x => BuildNode(x, planParameters, rootParameters)).ToList();
				return new MethodCallNode(methodCallExpression.Object is null ? null : BuildNode(methodCallExpression.Object, planParameters, rootParameters), methodDescriptor, parameters);

			case NewArrayExpression newArrayExpression:
				return new NewArrayNode(newArrayExpression.Type.GetElementType()!.AssemblyQualifiedName!, [..newArrayExpression.Expressions.Select(x => BuildNode(x, planParameters, rootParameters))]);

			case ListInitExpression listInitExpression:
				return new ListInitNode
				(
					(NewNode) BuildNode(listInitExpression.NewExpression, planParameters, rootParameters),
					[..listInitExpression.Initializers.Select(init => new ElementInitNode(MethodDescriptor.Create(init.AddMethod), [..init.Arguments.Select(x => BuildNode(x, planParameters, rootParameters))]))]
				);

			case MemberInitExpression memberInitExpression:
				// todo: consider adding support for MemberListBinding and MemberMemberBinding, but they are less common in projections. For now, we only support MemberAssignment.
				if (memberInitExpression.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Unsupported member binding type: {memberInitExpression.Bindings.First().BindingType}");
				}

				return new MemberInitNode
				(
					(NewNode) BuildNode(memberInitExpression.NewExpression, planParameters, rootParameters),
					[..memberInitExpression.Bindings.Cast<MemberAssignment>().Select(b => new MemberAssignmentNode(BuildNode(b.Expression, planParameters, rootParameters)))]
				);

			case NewExpression newExpression:
				return new NewNode([..newExpression.Arguments.Select(x => BuildNode(x, planParameters, rootParameters))]);

			case DefaultExpression defaultExpression:
				return (typeof(DefaultNode<>).MakeGenericType(defaultExpression.Type).GetConstructor([])!.Invoke([]) as INode)!;

			case ParameterExpression parameterExpression:
				return (typeof(ParameterNode<>).MakeGenericType(parameterExpression.Type).GetConstructor([typeof(string), typeof(string)])!.Invoke(["0", parameterExpression.Name]) as INode)!;

			case LambdaExpression lambdaExpression:
				return new LambdaExpressionNode(BuildNode(lambdaExpression.Body, planParameters, rootParameters),
					lambdaExpression.Parameters.Select((p, i) => typeof(ParameterNode<>).MakeGenericType(p.Type).GetConstructor([typeof(string), typeof(string)])!.Invoke([i.ToString(), p.Name]) as IParameterNode).ToArray());

			default:
				throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
		}
	}
}