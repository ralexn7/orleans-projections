using System.Reflection;

namespace Orleans.Projections.Serialization;

[GenerateSerializer]
public record MemberDescriptor (string DeclaringType, string Name)
{
    public static MemberDescriptor Create (MemberInfo member) =>
        new(DeclaringType: member.DeclaringType!.AssemblyQualifiedName!, Name: member.Name);

    public MemberInfo Resolve ()
    {
        var declaringType = Type.GetType(DeclaringType, throwOnError: true)!;

        return declaringType.GetMember(Name, BindingFlags.Public | BindingFlags.Instance).Single();
    }
}