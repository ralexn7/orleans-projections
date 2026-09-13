using System.Linq.Expressions;
using Orleans.Projections.Nodes;
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

        var request = new ProjectionPlan<PersonState>([
            new PropertyNode(["Name"]),
            new PropertyNode(["Age"]),
        ]);

        var result = await grain.GetProjection(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "Ada", 42 }));
    }

    [Test]
    public async Task Projects_Nested_Members ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));

        var request = new ProjectionPlan<PersonState>([
            new PropertyNode(["Address", "City"]),
            new PropertyNode(["Address", "Country"]),
        ]);

        var result = await grain.GetProjection(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "London", "UK" }));
    }

    [Test]
    public async Task Projects_Constants_And_Members_Together ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));

        var request = new ProjectionPlan<PersonState>([
            new ConstNode("literal"),
            new PropertyNode(["Name"]),
            new ConstNode(7),
            new UnaryExpressionNode(new GenericPropertyNode<int>(["Age"]), ExpressionType.Negate),
            new BinaryExpressionNode(new GenericPropertyNode<int>(["Age"]), new GenericConstNode<int>(5), ExpressionType.Subtract)
        ]);

        var result = await grain.GetProjection(request, CancellationToken.None);

        Assert.That(result.Values, Is.EqualTo(new object?[] { "literal", "Ada", 7 }));
    }
    
    [Test]
    public async Task Projects_AnonymousType_Entire_Flow ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));
        
        var data = await grain.Project(state => new
        {
            state.Name,
            state.Age,
            Location = $"{state.Address.City}, {state.Address.Country}",
            AgePlusFive = state.Age + 5,
            IsAdult = state.Age >= 18
        });
        
        Assert.That(data.Name, Is.EqualTo("Ada"));
        Assert.That(data.Age, Is.EqualTo(42));
        Assert.That(data.Location, Is.EqualTo("London, UK"));
        Assert.That(data.AgePlusFive, Is.EqualTo(47));
        Assert.That(data.IsAdult, Is.EqualTo(true));
    }

    public class PersonData
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Location { get; set; }
        public int AgePlusFive { get; set; }
        public bool IsAdult { get; set; }
    }
    
    [Test]
    public async Task Projects_ExplicitType_Entire_Flow ()
    {
        var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK")));
        
        var data = await grain.Project(state => new PersonData
        {
            Name = state.Name,
            Age = state.Age,
            Location = $"{state.Address.City}, {state.Address.Country}",
            AgePlusFive = state.Age + 5,
            IsAdult = state.Age >= 18
        });
        
        Assert.That(data.Name, Is.EqualTo("Ada"));
        Assert.That(data.Age, Is.EqualTo(42));
        Assert.That(data.Location, Is.EqualTo("London, UK"));
        Assert.That(data.AgePlusFive, Is.EqualTo(47));
        Assert.That(data.IsAdult, Is.EqualTo(true));
    }
}