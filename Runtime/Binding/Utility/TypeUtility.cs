#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Bodardr.Databinding.Runtime
{
    public static class TypeUtility
    {
        public static bool TryGetType(string assemblyQualifiedTypeName, out Type foundType)
        {
            foundType = Type.GetType(assemblyQualifiedTypeName);

            if (foundType != null)
                return true;

            var typeName = assemblyQualifiedTypeName.Split(',')[0].Split('.')[^1];

            if (typeName.Contains('+'))
                typeName = typeName.Split('+')[^1];

            foundType = TypeCache.GetTypesWithAttribute<FormerlySerializedAsBindingAttribute>().FirstOrDefault(x =>
                x.GetCustomAttribute<FormerlySerializedAsBindingAttribute>().OldName == typeName);

            return foundType != null;
        }
    }
}
#endif
