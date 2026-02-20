#if UNITY_EDITOR
using System;
using System.ComponentModel;
using System.Reflection;

public struct BindingPropertyEntry
{
    private string assemblyName;

    public readonly bool IsStatic;
    public readonly bool IsDynamic;

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
        IsDynamic = typeof(INotifyPropertyChanged).IsAssignableFrom(type);
        IsStatic = type.IsAbstract && type.IsSealed;
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
#endif