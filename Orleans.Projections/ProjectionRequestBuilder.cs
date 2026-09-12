using System.Linq.Expressions;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public class ProjectionRequestBuilder
{
	public static ProjectionRequest Build<TState, TProjection> (Expression<Func<TState, TProjection>> projectionExpression)
	{
		var nodes = new List<INode>();

		if (projectionExpression.Body is not NewExpression newExpression)
		{
			throw new ArgumentException("Projection expression must be a NewExpression.", nameof(projectionExpression));
		}
		
		var arguments = newExpression.Arguments;

		foreach (var arg in arguments)
		{
			nodes.Add(BuildNode(arg));
		}
		
		return new ProjectionRequest([..nodes]);
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
				return new MemberInitNode
				(
					(NewNode) BuildNode(memberInitExpression.NewExpression),
					[..memberInitExpression.Bindings.Cast<MemberAssignment>().Select(b => new MemberAssignmentNode(MemberDescriptor.Create(b.Member), BuildNode(b.Expression)))]
				);

			case NewExpression newExpression:
				return new NewNode(ConstructorDescriptor.Create(newExpression.Constructor!), [..newExpression.Arguments.Select(BuildNode)]);

			default:
				throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
		}
	}
}