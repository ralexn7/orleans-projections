using System.Reflection;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MethodDescriptor (string DeclaringType, string Name, string[] ParameterTypes, string[] GenericArguments)
{
    public static MethodDescriptor Create (MethodInfo method)
    {                                
        var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

        return new MethodDescriptor(DeclaringType: method.DeclaringType!.AssemblyQualifiedName!, Name: method.Name,
            ParameterTypes: [..definition.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName!)],
            GenericArguments: method.IsGenericMethod
                ? [..method.GetGenericArguments().Select(t => t.AssemblyQualifiedName!)]
                : []);
    }

    public MethodInfo Resolve ()
    {
        var declaringType = Type.GetType(DeclaringType, throwOnError: true)!;
        var parameterTypes = ParameterTypes.Select(t => Type.GetType(t, true)!).ToArray();

        if (GenericArguments.Length == 0)
        {        
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
}