using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

public interface IParameterNode : INode
{
    ParameterExpression New ();
}

[GenerateSerializer]
public record ParameterNode<T> (string SequentialNumber, string? Name) : IParameterNode
{
    public Expression BuildExpression (BuildContext context)
    {
        if (context.CurrentLambda == null)
        {
            throw new InvalidOperationException("ParameterNode can only be used within a lambda expression.");
        }

        return context.CurrentLambda.GetParameter(SequentialNumber);
    }
    
    public ParameterExpression New () => Expression.Parameter(typeof(T), Name);
}
