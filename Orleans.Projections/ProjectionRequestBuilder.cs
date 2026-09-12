using System.Linq.Expressions;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public class ProjectionRequestBuilder
{
    public static ProjectionRequest Build<TState, TProjection> (Expression<Func<TState, TProjection>> projectionExpression)
    {
        var members = new List<INode>();
        
        return new ProjectionRequest([..members]);
    }
}