using System.Reflection;

namespace Orleans.Projections;

[GenerateSerializer]
public record Projection (object?[] Values)
{
	public T ConvertToInstance<T> ()
	{
		return (T)ConvertToInstance(typeof(T), Values);
	}
	
	private static object ConvertToInstance (Type type, object?[] values)
	{
		// todo: add reflection cache to avoid repeated reflection calls for the same type
		
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

		return ConvertToInstance(targetType, values);
	}
}