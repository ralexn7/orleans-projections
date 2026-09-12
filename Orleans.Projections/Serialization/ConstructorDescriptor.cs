using System.Reflection;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record ConstructorDescriptor (string DeclaringType, string[] ParameterTypes)
{
    public static ConstructorDescriptor Create (ConstructorInfo constructor) =>
        new(DeclaringType: constructor.DeclaringType!.AssemblyQualifiedName!,
            ParameterTypes: [..constructor.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName!)]);

    public ConstructorInfo Resolve ()
    {
        var declaringType = Type.GetType(DeclaringType, throwOnError: true)!;
        var parameterTypes = ParameterTypes.Select(t => Type.GetType(t, true)!).ToArray();

        return declaringType.GetConstructor(parameterTypes) ??
               throw new MissingMethodException(declaringType.FullName, ".ctor");
    }
}