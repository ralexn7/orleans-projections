using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record ElementInitNode (MethodDescriptor AddMethod, List<INode> Arguments);

[GenerateSerializer]
public record ListInitNode (NewNode NewExpression, List<ElementInitNode> Initializers) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        var newExpression = (NewExpression) NewExpression.BuildExpression(context);

        var initializers = Initializers.Select(init =>
            Expression.ElementInit(init.AddMethod.Resolve(), init.Arguments.Select(a => a.BuildExpression(context))));

        return Expression.ListInit(newExpression, initializers);
    }
}