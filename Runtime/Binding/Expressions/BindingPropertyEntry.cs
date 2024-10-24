using System;
using System.Reflection;

public struct BindingPropertyEntry
{
    private string assemblyName;

    [NonSerialized]
    public Type Type;

    [NonSerialized]
    public MemberInfo MemberInfo;

    public string MemberName;
    public string AssemblyQualifiedTypeName;

    public bool TypeOnly => string.IsNullOrEmpty(MemberName);
    public string DisplayName => TypeOnly ? Type.Name : MemberName;
    public string Name => TypeOnly ? Type.Name : MemberName;

    public BindingPropertyEntry(Type type)
    {
        MemberName = string.Empty;
        Type = type;
        AssemblyQualifiedTypeName = Type.AssemblyQualifiedName;
        assemblyName = Type.Assembly.GetName().Name;
        MemberInfo = null;
    }

    public BindingPropertyEntry(Type type, string memberName) : this(type)
    {
        MemberName = memberName;
    }
    
    public BindingPropertyEntry(Type type, string memberName, MemberInfo memberInfo) : this(type, memberName)
    {
        MemberInfo = memberInfo;
    }
}
