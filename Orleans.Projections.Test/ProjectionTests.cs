using System.Linq.Expressions;
using Orleans.Projections.Serialization;
using Orleans.TestingHost;

namespace Orleans.Projections.Test;

public class ProjectionTests
{
    private TestCluster _cluster = null!;

    [OneTimeSetUp]
    public async Task SetUp ()
    {
        var builder = new TestClusterBuilder(initialSilosCount: 1);
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown ()
    {
        await _cluster.StopAllSilosAsync();
        await _cluster.DisposeAsync();
    }

    private async Task<IPersonGrain> GetPersonAsync (PersonState state)
    {
        var grain = _cluster.GrainFactory.GetGrain<IPersonGrain>(Guid.NewGuid().ToString());
        await grain.SetStateAsync(state);
        return grain;
    }

    [Test]
    public async Task Projects_TopLevel_Members ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));

        var request = new ProjectionRequest<PersonState>([
            new PropertyNode(["Name"]),
            new PropertyNode(["Age"]),
        ]);

        var result = await grain.ProjectAsync(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "Ada", 42 }));
    }

    [Test]
    public async Task Projects_Nested_Members ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));

        var request = new ProjectionRequest<PersonState>([
            new PropertyNode(["Address", "City"]),
            new PropertyNode(["Address", "Country"]),
        ]);

        var result = await grain.ProjectAsync(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "London", "UK" }));
    }

    [Test]
    public async Task Projects_Constants_And_Members_Together ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));

        var request = new ProjectionRequest<PersonState>([
            new ConstNode("literal"),
            new PropertyNode(["Name"]),
            new ConstNode(7),
            new UnaryExpressionNode(new GenericPropertyNode<int>(["Age"]), ExpressionType.Negate),
            new BinaryExpressionNode(new GenericPropertyNode<int>(["Age"]), new GenericConstNode<int>(5), ExpressionType.Subtract)
        ]);

        var result = await grain.ProjectAsync(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "literal", "Ada", 7 }));
    }
}