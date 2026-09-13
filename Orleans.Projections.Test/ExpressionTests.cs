using System.Linq;
using System.Linq.Expressions;

namespace Orleans.Projections.Test;

public class ExpressionTests
{
	[Test]
	public void Test()
	{
		Expression<Func<PersonState, object>> projection = state => new
		{
			// Member access (property), incl. nested paths
			state.Name,
			state.Age,
			state.Address.City,
			state.Address.Country,

			// Constant
			Const = 10,

			// Binary arithmetic (Add), and a more complex arithmetic tree (Multiply/Add/Modulo)
			Calc = state.Age + 5,
			Complex = (state.Age * 2 + 5) % 4,

			// Unary negation
			Negated = -state.Age,

			// Unary convert / cast
			AgeAsLong = (long) state.Age,

			// Boxing convert
			Boxed = (object) state.Age,

			// Comparison (GreaterThan) -> bool
			IsAdult = state.Age > 18,

			// Logical AndAlso / OrElse, with a nested member (string.Length)
			IsNamedAdult = state.Age >= 18 && state.Name.Length > 0,

			// Conditional (ternary)
			AgeBand = state.Age >= 18 ? "adult" : "minor",

			// Null-coalesce
			Coalesced = state.Name ?? "unknown",

			// String concatenation (Add on strings -> string.Concat)
			Full = state.Name + " from " + state.Address.City,

			// Interpolated string (-> string.Format)
			Interpolated = $"{state.Name} ({state.Age})",

			// Static method call
			Func = int.Max(10, state.Age),

			// Instance method call (no args, and with args)
			Upper = state.Name.ToUpper(),
			Initial = state.Name.Substring(0, 1),

			// Indexer (string -> get_Chars)
			FirstLetter = state.Name[0],

			// Array init with mixed constant/member/computed elements
			Array = new int[]
			{
				state.Age,
				10,
				state.Age + 5
			},

			// List init
			Numbers = new List<int>
			{
				state.Age,
				10,
				state.Age + 5
			},

			// Dictionary init (ListInit with multi-arg ElementInit)
			Lookup = new Dictionary<string, int>
			{
				{ "age", state.Age },
				{ "next", state.Age + 1 }
			},

			// New with constructor call
			NewAddress = new Address(state.Address.City, "US"),

			// Member-init on a mutable type
			Point = new Coordinate
			{
				X = state.Age,
				Y = state.Age + 1
			},

			// Nested anonymous type (nested projection)
			Obj = new
			{
				state.Name,
				state.Age
			}
		};
		
		var request = ProjectionRequestBuilder.Build(projection);
		
		Assert.That(request.Nodes.Count, Is.EqualTo(6));
	}
	
	[Test]
	public void MethodCall_TwoWayConvert ()
	{
		var state = new PersonState(
			Name: "Alice",
			Age: 30,
			Address: new Address("New York", "USA")
		);
		
		Expression<Func<PersonState, object>> projection = state => new
		{
			Upper = state.Name.ToUpper(),
			Initial = state.Name.Substring(0, 1),
			Location = $"{state.Address.City}, {state.Address.Country}",
			ALetters = state.Name.Where(c => c == 'A').ToArray()
		};
		
		var request = ProjectionRequestBuilder.Build(projection);
		
		Assert.That(request.Nodes.Count, Is.EqualTo(4));

		Expression<Func<PersonState, object?[]>> expression = request.BuildExpression();
		
		var res = expression.Compile()(state);
		
		Assert.That(res[0], Is.EqualTo("ALICE"));
		Assert.That(res[1], Is.EqualTo("A"));
		Assert.That(res[2], Is.EqualTo("New York, USA"));
		Assert.That(res[0], Is.EqualTo(new[] { 'A' }));
	}

	[Test]
	public void Lambda_InspectShape ()
	{
		// A projection containing a nested lambda: Enumerable.Where/Select over the string's chars.
		// The C# compiler emits each LINQ operator as a MethodCallExpression whose delegate argument
		// is a UnaryExpression(Quote) wrapping a LambdaExpression — the shape a future LambdaNode
		// (and a parameter-binding scope) would have to round-trip.
		Expression<Func<PersonState, object>> projection = state => new
		{
			Initials = state.Name.Where(c => c != ' ').Select(c => char.ToUpperInvariant(c))
		};

		// Pull the interesting sub-expressions into locals so they show up in the debugger's Locals pane.
		var newExpression = (NewExpression) projection.Body;
		var selectCall = (MethodCallExpression) newExpression.Arguments[0]; // Enumerable.Select(...)
		var whereCall = (MethodCallExpression) selectCall.Arguments[0];     // Enumerable.Where(...)

		// The delegate argument is wrapped in a Quote unary node; its operand is the actual lambda.
		var quotedSelector = (UnaryExpression) selectCall.Arguments[1];     // NodeType == Quote
		var selectorLambda = (LambdaExpression) quotedSelector.Operand;     // c => char.ToUpperInvariant(c)
		var selectorParam = selectorLambda.Parameters[0];                  // ParameterExpression 'c'
		var selectorBody = selectorLambda.Body;                            // MethodCallExpression: char.ToUpperInvariant(c)

		var quotedPredicate = (UnaryExpression) whereCall.Arguments[1];     // NodeType == Quote
		var predicateLambda = (LambdaExpression) quotedPredicate.Operand;   // c => c != ' '

		// Put a breakpoint on the line below and inspect the locals above.
		Assert.That(quotedSelector.NodeType, Is.EqualTo(ExpressionType.Quote));
		Assert.That(quotedPredicate.NodeType, Is.EqualTo(ExpressionType.Quote));
		Assert.That(selectorLambda.Parameters.Count, Is.EqualTo(1));
		Assert.That(selectorParam.Name, Is.EqualTo("c"));
		Assert.That(whereCall.Method.Name, Is.EqualTo("Where"));
		Assert.That(selectCall.Method.Name, Is.EqualTo("Select"));

		// Note: the inner lambda parameter 'c' is distinct from the root 'state' parameter.
		// ReferenceEquals(selectorParam, predicateLambda.Parameters[0]) is false — each lambda
		// declares its own parameter instance, which is exactly why a shared parameter-binding
		// scope is needed before this tree can be rebuilt on the grain side.
		Assert.That(ReferenceEquals(selectorParam, predicateLambda.Parameters[0]), Is.False);
	}
}

public class Coordinate                                                                       
{                                                                                             
	public int X { get; set; }                                                                
	public int Y { get; set; }                                                                
}     