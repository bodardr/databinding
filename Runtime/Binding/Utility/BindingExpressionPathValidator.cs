using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Serialization;

namespace Bodardr.Databinding.Runtime
{
    public static class BindingExpressionPathValidator
    {
        public static bool TryFixingPath<T>(SerializedProperty serializedProperty, BindingExpression<T> expression) where T : Delegate
        {
            bool pathHasChanged = false;
            var type = Type.GetType(expression.AssemblyQualifiedTypeNames[0]);
            var splitPath = expression.Path.Split('.');

            for (int i = 1; i < expression.AssemblyQualifiedTypeNames.Length; i++)
            {
                var allMembers =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                var memberInfos = type.GetMember(splitPath[i]);
                MemberInfo memberInfo;

                //No matching member with name, type.
                if (memberInfos.Length < 1)
                {
                    //Tries finding a FormerlySerializedAs attribute marker
                    memberInfo = allMembers.FirstOrDefault(x =>
                        x.GetCustomAttributes<FormerlySerializedAsBindingAttribute>()
                            .Any(y => y.OldName.Equals(splitPath[i]))
                        || x.GetCustomAttributes<FormerlySerializedAsAttribute>().Any(y => y.oldName == splitPath[i]));

                    if (memberInfo == null)
                    {
                        //Could not fix the path.
                        return false;
                    }

                    //If a formerly serialized attribute has been found, we have to rewire the path with the correct name
                    splitPath[i] = memberInfo.Name;
                    expression.Path = string.Join('.', splitPath);
                    expression.AssemblyQualifiedTypeNames[i] = (memberInfo!.MemberType switch
                    {
                        MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                        _ => ((FieldInfo)memberInfo).FieldType
                    }).AssemblyQualifiedName;
                    pathHasChanged = true;
                }
                else
                {
                    memberInfo = memberInfos[0];
                }

                var memberType = memberInfo!.MemberType switch
                {
                    MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                    _ => ((FieldInfo)memberInfo).FieldType
                };

                var hierarchyType = Type.GetType(expression.AssemblyQualifiedTypeNames[i]);
                if (memberType != hierarchyType)
                    return false;

                type = memberType;
            }

            if (pathHasChanged)
            {
                serializedProperty.boxedValue = expression;
                EditorUtility.SetDirty(serializedProperty.serializedObject.targetObject);
                serializedProperty.serializedObject.ApplyModifiedProperties();
            }
            
            //Returns true if the path is valid or has been fixed.
            return true;
        }
    }
}
