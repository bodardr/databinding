using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Serialization;

namespace Bodardr.Databinding.Runtime
{
    public static class BindingExpressionPathValidator
    {
        public static void TryFixingPath<T>(SerializedProperty serializedProperty, BindingExpression<T> expression)
            where T : Delegate
        {
            var expressionModified = FixPathInternal(expression, out _);

            if (!expressionModified)
                return;

            serializedProperty.boxedValue = expression;
            EditorUtility.SetDirty(serializedProperty.serializedObject.targetObject);
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
        public static BindingExpressionErrorContext TryFixingPath<T>(BindingListenerBase bindingListenerBase,
            BindingExpression<T> expression)
            where T : Delegate
        {
            var expressionModified = FixPathInternal(expression, out var errorContext);

            if (!expressionModified)
                return errorContext;

            EditorUtility.SetDirty(bindingListenerBase);
            using var serializedObject = new SerializedObject(bindingListenerBase);
            serializedObject.ApplyModifiedProperties();

            return errorContext;
        }

        private static bool FixPathInternal<T>(BindingExpression<T> expression,
            out BindingExpressionErrorContext errorContext) where T : Delegate
        {
            //If the expression is empty, then there's nothing to fix.
            if (string.IsNullOrEmpty(expression.Path) || expression.AssemblyQualifiedTypeNames == null ||
                expression.AssemblyQualifiedTypeNames.Length == 0)
            {
                errorContext = BindingExpressionErrorContext.OK;
                return true;
            }
            
            var splitPath = expression.Path.Split('.');
            var hasFoundType = TypeUtility.TryGetType(expression.AssemblyQualifiedTypeNames[0], out var type);

            if (!hasFoundType)
            {
                errorContext = new BindingExpressionErrorContext(
                    BindingExpressionErrorContext.ErrorType.BINDING_NODE_TYPE_MISMATCH);
                return false;
            }

            var expressionModified = expression.AssemblyQualifiedTypeNames[0] != type.AssemblyQualifiedName;
            if (expressionModified)
            {
                expression.AssemblyQualifiedTypeNames[0] = type.AssemblyQualifiedName;
                splitPath[0] = type.Name;
                expression.Path = string.Join('.', splitPath);
            }

            for (int i = 1; i < expression.AssemblyQualifiedTypeNames.Length; i++)
            {
                var foundHierarchyType =
                    TypeUtility.TryGetType(expression.AssemblyQualifiedTypeNames[i], out var hierarchyType);

                var allMembers =
                    type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                //So first we try finding a matching member with the name.
                var memberInfos = type.GetMember(splitPath[i]);
                MemberInfo memberInfo;

                //If there is no matching member with name OR
                //If the member hierarchy type hasn't been found OR
                //Doesn't match the type.
                if (memberInfos.Length < 1 || !foundHierarchyType ||
                    !expression.AssemblyQualifiedTypeNames[i].Equals(hierarchyType.AssemblyQualifiedName))
                {
                    //We try finding a FormerlySerializedAs attribute marker containing the old name.
                    var formerlySerializedMember = allMembers.FirstOrDefault(x =>
                        x.GetCustomAttributes<FormerlySerializedAsBindingAttribute>()
                            .Any(y => y.OldName.Equals(splitPath[i]))
                        || x.GetCustomAttributes<FormerlySerializedAsAttribute>().Any(y => y.oldName == splitPath[i]));

                    var modifyMember = false;

                    //If it exists, it is the new memberInfo.
                    //The path has to be modified in consequence.
                    if (formerlySerializedMember != null)
                    {
                        memberInfo = formerlySerializedMember;
                    }
                    //If no formerly serialized member has been found.
                    //We try replacing with the first found member matching the name 
                    else if (memberInfos.Length >= 1)
                    {
                        memberInfo = memberInfos[0];
                    }
                    //If no member matches the name and type, and no attribute was defined,
                    //There's nothing more we can do and the path is invalid.
                    else
                    {
                        errorContext = new BindingExpressionErrorContext(
                            BindingExpressionErrorContext.ErrorType.COULD_NOT_FIND_MEMBER,
                            $"Could not find member with name {splitPath[i]} in {expression.Path}");
                        return false;
                    }

                    //At this point, either the name or type have swapped, so we need to
                    //modify the expression path and assembly type names to match the fix.
                    splitPath[i] = memberInfo.Name;
                    expression.Path = string.Join('.', splitPath);
                    expression.AssemblyQualifiedTypeNames[i] = (memberInfo!.MemberType switch
                    {
                        MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                        _ => ((FieldInfo)memberInfo).FieldType
                    }).AssemblyQualifiedName;
                    expressionModified = true;
                }

                type = hierarchyType;
            }

            errorContext = BindingExpressionErrorContext.OK;
            return expressionModified;
        }
    }
}
