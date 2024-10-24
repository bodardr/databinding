using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bodardr.Databinding.Editor
{
    public static class TypeExtensions
    {
        public static List<Type> TypeCache;

        static TypeExtensions()
        {
            TypeCache = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToList();
        }

        public static IEnumerable<MemberInfo> FindFieldsAndProperties(this Type type,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
        {
            var fieldInfos = type.GetFields(bindingFlags).Cast<MemberInfo>();
            return fieldInfos.Concat(type.GetProperties(bindingFlags));
        }

        public static Type GetPropertyOrFieldType(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                _ => ((FieldInfo)memberInfo).FieldType
            };
        }

        public static bool IsStaticType(this Type type)
        {
            if (type == null)
                return false;

            return type.IsAbstract && type.IsSealed;
        }

        /// <summary>
        /// Taken from : https://stackoverflow.com/questions/1749966/c-sharp-how-to-determine-whether-a-type-is-a-number
        /// </summary>
        /// <param name="o"></param>
        /// <returns>if it is a numeric type</returns>
        public static bool IsNumericType(this Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
