using System.Linq.Expressions;
using System.Reflection;
using Orleans.Projections.Serialization;

namespace Orleans.Projections;

public interface IProjectableGrain<TState> : IGrain
{
	Task<ProjectionResult> ProjectAsync (ProjectionRequest<TState> request, CancellationToken cancellationToken);
}

[GenerateSerializer]
public record ProjectionRequest<TState> (INode[] Nodes)
{
	public Expression<Func<TState, object?[]>> BuildExpression ()
	{
		List<Expression> accessors = [];
		var parameter = Expression.Parameter(typeof(TState), "state");
		
		var buildContext = new BuildContext(parameter);
		
		accessors.AddRange(Nodes.Select(node => node.BuildExpression(buildContext)));
		
		var arrayExpression = Expression.NewArrayInit(typeof(object), accessors.Select(e => Expression.Convert(e, typeof(object))));
		
		var projectorExpression = Expression.Lambda<Func<TState, object?[]>>(arrayExpression, parameter);

		return projectorExpression;
	}
}

[GenerateSerializer]
public record ProjectionResult (object?[] Values);

public static class ProjectableGrainExtensions
{
	public static async Task<TProjection> Project<TState, TProjection> (this IProjectableGrain<TState> grain, Expression<Func<TState, TProjection>> projection, CancellationToken cancellationToken = default)
	{
		var request = ProjectionRequestBuilder.Build(projection);
		ProjectionResult result = await grain.ProjectAsync(request, cancellationToken);
		
		// map the result values to the projection type
		
		return (TProjection)Map(typeof(TProjection), result.Values);
	}
	
	public static object Map (Type type, object?[] values)
	{
		// first try to find a constructor that matches the number of values and their types
		var ctors = type
			.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
			.Where(c => c.GetParameters().Length == values.Length);

		foreach (var ctor in ctors)
		{
			var parameters = ctor.GetParameters();
			var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

			bool match = true;
			for (var i = 0; i < parameterTypes.Length; i++)
			{
				var paramType = parameterTypes[i];
				var valueType = values[i]?.GetType();

				if (valueType is null)
				{
					// if it is nullable or reference type, null is ok; otherwise it doesn't match
					if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) is null)
					{
						match = false;
						break;
					}
				}
				else if (!paramType.IsAssignableFrom(valueType))
				{
					match = false;
					break;
				}
			}

			if (match)
			{
				var mappedValues = new object?[values.Length];
				for (var i = 0; i < values.Length; i++)
				{
					mappedValues[i] = MapValue(parameterTypes[i], values[i]);
				}

				return ctor.Invoke(mappedValues);
			}
		}
		
		// if constructor isn't found, try to create an instance and set properties
		var properties = type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanWrite)
			.OrderBy(p => p.MetadataToken)
			.ToArray();

		if (properties.Length != values.Length)
		{
			throw new ArgumentException($"Expected {properties.Length} values, got {values.Length} for {type.Name}.");
		}
		
		var instance = Activator.CreateInstance(type)
		               ?? throw new InvalidOperationException($"Cannot create {type}.");

		for (var i = 0; i < properties.Length; i++)
		{
			var property = properties[i];
			var value = MapValue(property.PropertyType, values[i]);

			property.SetValue(instance, value);
		}

		return instance;
	}

	private static object? MapValue (Type targetType, object? value)
	{
		if (value is null)
		{
			return null;
		}

		// object[] can mean either an array of values or a single nested complex object
		if (value is not object?[] values)
		{
			return value;
		}

		if (targetType.IsArray)
		{
			var elementType = targetType.GetElementType()
			                  ?? throw new InvalidOperationException(
				                  $"Cannot determine element type of {targetType}.");

			var array = Array.CreateInstance(elementType, values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				var element = MapValue(elementType, values[i]);
				array.SetValue(element, i);
			}

			return array;
		}

		return Map(targetType, values);
	}
}
