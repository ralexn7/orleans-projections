using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record NewNode (List<INode> Arguments) : INode
{
    public Expression BuildExpression (BuildContext context)
    {
        // todo: consider preserving .ctor parameters
        return Expression.NewArrayInit(typeof(object), Arguments.Select(a => Expression.Convert(a.BuildExpression(context), typeof(object))));
    }
}