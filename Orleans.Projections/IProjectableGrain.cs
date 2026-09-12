using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<ProjectionResult> ProjectAsync (ProjectionRequest request, CancellationToken cancellationToken);
}

[GenerateSerializer]
public record ProjectionRequest (INode[] Nodes);

[GenerateSerializer]
public record ProjectionResult (object?[] Values);
