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
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));

		var request = new ProjectionPlan<PersonState>([
			new PropertyNode(["Name"]),
			new PropertyNode(["Age"]),
		]);

		var result = await grain.Get(request, [], CancellationToken.None);

		Assert.That(result.Values, Is.EqualTo(new object?[] { "Ada", 42 }));
	}

	[Test]
	public async Task Projects_Nested_Members ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));

		var request = new ProjectionPlan<PersonState>([
			new PropertyNode(["Address", "City"]),
			new PropertyNode(["Address", "Country"]),
		]);

		var result = await grain.Get(request, [], CancellationToken.None);

		Assert.That(result.Values, Is.EqualTo(new object?[] { "London", "UK" }));
	}

	[Test]
	public async Task Projects_Constants_And_Members_Together ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));

		var request = new ProjectionPlan<PersonState>([
			new ConstNode(0),
			new PropertyNode(["Name"]),
			new ConstNode(1),
			new UnaryExpressionNode(new GenericPropertyNode<int>(["Age"]), ExpressionType.Negate),
			new BinaryExpressionNode(new GenericPropertyNode<int>(["Age"]), new GenericConstNode<int>(2), ExpressionType.Subtract)
		]);

		var result = await grain.Get(request, ["literal", 7, 5], CancellationToken.None);

		Assert.That(result.Values, Is.EqualTo(new object?[] { "literal", "Ada", 7, -42, 37 }));
	}
	
	[Test]
	public async Task Projects_AnonymousType_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		var data = await grain.Get(state => new
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
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		var data = await grain.Get(state => new PersonData
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
	
	[Test]
	public async Task Projects_Scalar_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		var data = await grain.Get(state => state.Age);
		
		Assert.That(data, Is.EqualTo(42));
	}
	
	[Test]
	public async Task Projects_ScalarLambda_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		var data = await grain.Get(state => state.Age + 20);
		
		Assert.That(data, Is.EqualTo(62));
	}
	
	public enum AgeCategory
	{
		Child = 1,
		Adult = 2
	}
	
	[Test]
	public async Task Projects_ScalarEnum_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		var data = await grain.Get(state => state.Age >= 18 ? AgeCategory.Adult : AgeCategory.Child);
		
		Assert.That(data, Is.EqualTo(AgeCategory.Adult));
	}
	
	[Test]
	public async Task Projects_RawState_InvalidOperationException ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), []));
		
		Assert.ThrowsAsync<InvalidOperationException>(async () => await grain.Get(state => state));
	}
		
	[Test]
	public async Task Projects_LambdaCollection1_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), ["tag1", "tag2"]));
		
		var data = await grain.Get(state => state.Tags.Select(tag => $"{state.Name}-{tag}").ToList());
		
		Assert.That(data, Is.EqualTo(new List<string> { "Ada-tag1", "Ada-tag2" }));
	}
	
	[Test]
	public async Task Projects_LambdaCollection2_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), ["tag1", "tag2"]));
		
		var data = await grain.Get(state => state.Tags.Select(tag => tag.Length).ToList());
		
		Assert.That(data, Is.EqualTo(new List<int> { 4, 4 }));
	}
		
	[Test]
	public async Task Projects_StaticProperty_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), ["tag1", "tag2"]));
		
		var data = await grain.Get(state => new
		{
			Date = DateTime.Now,
			Number = int.MaxValue
		});
		
		Assert.That(data.Date, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
		Assert.That(data.Number, Is.EqualTo(int.MaxValue));
	}
			
	[Test]
	public async Task Projects_StaticField_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), ["tag1", "tag2"]));
		
		var data = await grain.Get(state => new
		{
			EmptyString = string.Empty,
			EmptyGuid = Guid.Empty
		});
		
		Assert.That(data.EmptyString, Is.EqualTo(string.Empty));
		Assert.That(data.EmptyGuid, Is.EqualTo(Guid.Empty));
	}
	
	[Test]
	public async Task Projects_InnerConstructor_Entire_Flow ()
	{
		var grain = await GetPersonAsync(new PersonState("Ada", 42, new Address("London", "UK"), ["tag1", "tag2"]));
		
		var data = await grain.Get(state => new
		{
			Data = new
			{
				state.Age,
				state.Name,
				Inner = new
				{
					state.Age,
					state.Name,
				}
			}
		});
		
		Assert.That(data.Data.Age, Is.EqualTo(42));
		Assert.That(data.Data.Name, Is.EqualTo("Ada"));
		Assert.That(data.Data.Inner.Age, Is.EqualTo(42));
		Assert.That(data.Data.Inner.Name, Is.EqualTo("Ada"));
	}
}