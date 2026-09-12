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
        
        var request = ProjectionRequestBuilder.Build<PersonState, object>(projection);
        
        Assert.That(request.Nodes.Count, Is.EqualTo(6));
    }
}

public class Coordinate                                                                       
{                                                                                             
    public int X { get; set; }                                                                
    public int Y { get; set; }                                                                
}     