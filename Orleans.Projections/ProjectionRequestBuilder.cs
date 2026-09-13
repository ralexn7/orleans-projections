using System.Linq.Expressions;
using Orleans.Projections.Nodes;

namespace Orleans.Projections;

public class ProjectionRequestBuilder
{
	public static ProjectionPlan<TState> Build<TState, TProjection> (Expression<Func<TState, TProjection>> projectionExpression)
	{
		var nodes = new List<INode>();
		
		IReadOnlyCollection<Expression> arguments;
		
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
		else
		{
			throw new ArgumentException("Projection expression body must be either a NewExpression or a MemberInitExpression.", nameof(projectionExpression));
		}

		foreach (var arg in arguments)
		{
			// Ensure that the argument is not a root parameter expression. Cause we do not want to expose the internal grain state as is
			// todo: Maybe it is not worth it. Returning raw state (readonly!) may be useful for some scenarios as Grain Dashboards
			if (arg is ParameterExpression)
			{
				throw new ArgumentException("Projection expression cannot contain root parameter expressions.", nameof(projectionExpression));
			}
			
			nodes.Add(BuildNode(arg));
		}
		
		return new ProjectionPlan<TState>([..nodes]);
	}
	
	private static INode BuildNode (Expression expression)
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
				
				return (typeof(GenericPropertyNode<>).MakeGenericType(memberExpression.Type).GetConstructor([typeof(string[])])!.Invoke([path.ToArray()]) as INode)!;

			case ConstantExpression constantExpression:
				return (typeof(GenericConstNode<>).MakeGenericType(constantExpression.Type).GetConstructor([constantExpression.Type])!.Invoke([constantExpression.Value]) as INode)!;
			
			case UnaryExpression unaryExpression:
				return (typeof(GenericUnaryExpressionNode<>).MakeGenericType(unaryExpression.Type).GetConstructor([
					typeof(INode), typeof(ExpressionType)])!.Invoke([BuildNode(unaryExpression.Operand), unaryExpression.NodeType]) as INode)!;
			
			case BinaryExpression binaryExpression:
				return new BinaryExpressionNode(BuildNode(binaryExpression.Left), BuildNode(binaryExpression.Right), binaryExpression.NodeType);
			
			case ConditionalExpression conditionalExpression:
				return new ConditionalNode(BuildNode(conditionalExpression.Test), BuildNode(conditionalExpression.IfTrue), BuildNode(conditionalExpression.IfFalse));
			
			case MethodCallExpression methodCallExpression:
				var methodDescriptor = MethodDescriptor.Create(methodCallExpression.Method);
				var parameters = methodCallExpression.Arguments.Select(BuildNode).ToList();
				return new MethodCallNode(methodCallExpression.Object is null ? null : BuildNode(methodCallExpression.Object), methodDescriptor, parameters);

			case NewArrayExpression newArrayExpression:
				return new NewArrayNode(newArrayExpression.Type.GetElementType()!.AssemblyQualifiedName!, [..newArrayExpression.Expressions.Select(BuildNode)]);

			case ListInitExpression listInitExpression:
				return new ListInitNode
				(
					(NewNode) BuildNode(listInitExpression.NewExpression),
					[..listInitExpression.Initializers.Select(init => new ElementInitNode(MethodDescriptor.Create(init.AddMethod), [..init.Arguments.Select(BuildNode)]))]
				);

			case MemberInitExpression memberInitExpression:
				// todo: consider adding support for MemberListBinding and MemberMemberBinding, but they are less common in projections. For now, we only support MemberAssignment.
				if (memberInitExpression.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Unsupported member binding type: {memberInitExpression.Bindings.First().BindingType}");
				}
				
				return new MemberInitNode
				(
					(NewNode) BuildNode(memberInitExpression.NewExpression),
					[..memberInitExpression.Bindings.Cast<MemberAssignment>().Select(b => new MemberAssignmentNode(BuildNode(b.Expression)))]
				);

			case NewExpression newExpression:
				return new NewNode([..newExpression.Arguments.Select(BuildNode)]);
			
			case DefaultExpression defaultExpression:
				return (typeof(DefaultNode<>).MakeGenericType(defaultExpression.Type).GetConstructor([])!.Invoke([]) as INode)!;
			
			case ParameterExpression parameterExpression:
				return (typeof(ParameterNode<>).MakeGenericType(parameterExpression.Type).GetConstructor([typeof(string), typeof(string)])!.Invoke(["0", parameterExpression.Name]) as INode)!;
			
			case LambdaExpression lambdaExpression:
				return new LambdaExpressionNode(BuildNode(lambdaExpression.Body),
					lambdaExpression.Parameters.Select((p, i) => typeof(ParameterNode<>).MakeGenericType(p.Type).GetConstructor([typeof(string), typeof(string)])!.Invoke([i.ToString(), p.Name]) as IParameterNode).ToArray());

			default:
				throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
		}
	}
}