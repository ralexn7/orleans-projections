using System.Reflection;

namespace Orleans.Projections.Nodes;

[GenerateSerializer]
public record MethodDescriptor (string DeclaringType, string Name, string[] ParameterTypes, string[] GenericArguments)
{
	public static MethodDescriptor Create (MethodInfo method)
	{                                
		var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

		var parameters = definition.GetParameters()
			.Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? p.ParameterType.Name).ToArray();
		
		var genericArgs = method.IsGenericMethod
			? method.GetGenericArguments().Select(t => t.AssemblyQualifiedName ?? t.FullName ?? t.Name).ToArray()
			: [];

		return new MethodDescriptor
		(
			DeclaringType: method.DeclaringType!.AssemblyQualifiedName!,
			Name: method.Name,
			ParameterTypes: parameters,
			GenericArguments: genericArgs
		);
	}

	public MethodInfo Resolve ()
	{
		var declaringType = Type.GetType(DeclaringType, throwOnError: true)!;

		if (GenericArguments.Length == 0)
		{
			var parameterTypes = ParameterTypes.Select(t => Type.GetType(t, true)!).ToArray();

			return declaringType.GetMethod(Name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
					   binder: null, types: parameterTypes, modifiers: null) ??
				   throw new MissingMethodException(declaringType.FullName, Name);
		}

		var genericArgs = GenericArguments.Select(t => Type.GetType(t, true)!).ToArray();

		var definition = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.Where(m => m.Name == Name && m.IsGenericMethodDefinition)
			.Where(m => m.GetGenericArguments().Length == genericArgs.Length)
			.First(m => m.GetParameters()
				.Select(p => p.ParameterType.Name)
				.SequenceEqual(ParameterTypes.Select(SimpleName)));

		return definition.MakeGenericMethod(genericArgs);
	}

	private static string SimpleName(string assemblyQualified) =>
		Type.GetType(assemblyQualified, false)?.Name ?? assemblyQualified;

	public virtual bool Equals (MethodDescriptor? other) =>
		other is not null
		&& DeclaringType == other.DeclaringType
		&& Name == other.Name
		&& ParameterTypes.SequenceEqual(other.ParameterTypes)
		&& GenericArguments.SequenceEqual(other.GenericArguments);

	public override int GetHashCode ()
	{
		var hash = new HashCode();
		hash.Add(DeclaringType);
		hash.Add(Name);
		foreach (var parameterType in ParameterTypes)
		{
			hash.Add(parameterType);
		}
		foreach (var genericArgument in GenericArguments)
		{
			hash.Add(genericArgument);
		}
		return hash.ToHashCode();
	}
}