namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ConstNode(object? Value) : INode;