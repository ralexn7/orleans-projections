using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ElementInitNode (MethodDescriptor AddMethod, List<INode> Arguments);

[GenerateSerializer]
public record ListInitNode (NewNode NewExpression, List<ElementInitNode> Initializers) : INode
{
    public Expression BuildExpression (ParameterExpression parameter)
    {
        var newExpression = (NewExpression) NewExpression.BuildExpression(parameter);

        var initializers = Initializers.Select(init =>
            Expression.ElementInit(init.AddMethod.Resolve(), init.Arguments.Select(a => a.BuildExpression(parameter))));

        return Expression.ListInit(newExpression, initializers);
    }
}