namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record PropertyNode (string[] Path) : INode;