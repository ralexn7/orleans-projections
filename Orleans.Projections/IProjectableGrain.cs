namespace Orleans.Projections;

public interface IProjectableGrain<TState>
{
    Task<ProjectionResult> ProjectAsync (ProjectionRequest request, CancellationToken cancellationToken);
}

[GenerateSerializer]
public record ProjectionRequest (ProjectionMember[] Members);

[GenerateSerializer]
public abstract record ProjectionMember;

[GenerateSerializer]
public record FromMember (string[] Paths) : ProjectionMember;

[GenerateSerializer]
public record FromConstantMember (object? Value) : ProjectionMember;

[GenerateSerializer]
public record ProjectionResult (object?[] Values);
