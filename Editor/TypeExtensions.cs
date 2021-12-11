using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bodardr.Databinding.Editor
{
    public static class TypeExtensions
    {
        public static List<Type> AllTypes;

        static TypeExtensions()
        {
            AllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsClass).ToList();
        }
        
        public static IEnumerable<MemberInfo> FindFieldsAndProperties(this Type type)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            var fieldInfos = type.GetFields(bindingFlags).Cast<MemberInfo>();
            return fieldInfos.Concat(type.GetProperties(bindingFlags));
        }

        public static Type GetPropertyOrFieldType(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Property)
                return ((PropertyInfo)memberInfo).PropertyType;
            return ((FieldInfo)memberInfo).FieldType;
        }
    }
}