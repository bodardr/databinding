using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public abstract class BindingExpressionWithLocation<TExpr> : BindingExpression<TExpr> where TExpr : Delegate
    {
        [SerializeField] protected BindingExpressionLocation location = BindingExpressionLocation.None;

        #if UNITY_EDITOR
        public override bool IsValid(BindingListenerBase context, BindingNode bindingNode,
            out BindingExpressionErrorContext errorCtx)
        {
            //Not enough TypeNames. Expression isn't filled.
            if (AssemblyQualifiedTypeNames.Length < 1)
            {
                errorCtx = new BindingExpressionErrorContext(BindingExpressionErrorContext.ErrorType.EMPTY_EXPRESSION,
                    "The expression is empty");
                return false;
            }

            var type = Type.GetType(AssemblyQualifiedTypeNames[0]);

            if (location == BindingExpressionLocation.InGameObject)
            {
                //todo verify this.
                if (!typeof(Component).IsAssignableFrom(type))
                {
                    errorCtx = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.HIERARCHY_TYPE_MISMATCH,
                        $"Expression targets GameObject, but the Input type {type.Name} is not a Component.");
                    return false;
                }

                if (!context.TryGetComponent(type, out _))
                {
                    errorCtx = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.NO_MATCHING_COMPONENT_IN_GAMEOBJECT,
                        $"GameObject {context.name} has no component of type {type.Name}");
                    return false;
                }
            }
            else if (location == BindingExpressionLocation.InBindingNode)
            {
                if (bindingNode != null && type != bindingNode.BindingType)
                {
                    errorCtx = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.BINDING_NODE_TYPE_MISMATCH,
                        $"Binding node is of type {(bindingNode.BindingType == null ? "null" : $"{bindingNode.BindingType.Name}")} but the path's input type is {type.Name}");
                    return false;
                }
            }

            var splitPath = path.Split('.');
            var hasPathChanged = false;

            for (int i = 1; i < AssemblyQualifiedTypeNames.Length; i++)
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
                        errorCtx = new BindingExpressionErrorContext(
                            BindingExpressionErrorContext.ErrorType.COULD_NOT_FIND_MEMBER,
                            $"Member {splitPath[i]} in type {type.Name} could not be found");
                        return false;
                    }

                    //If a formerly serialized attribute has been found, we have to rewire the path with the correct name
                    splitPath[i] = memberInfo.Name;
                    path = string.Join('.', splitPath);
                    AssemblyQualifiedTypeNames[i] = (memberInfo!.MemberType switch
                    {
                        MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                        _ => ((FieldInfo)memberInfo).FieldType
                    }).AssemblyQualifiedName;
                    hasPathChanged = true;
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

                //Member hierarchy type mismatch.
                var hierarchyType = Type.GetType(AssemblyQualifiedTypeNames[i]);
                if (memberType != hierarchyType)
                {
                    errorCtx = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.HIERARCHY_TYPE_MISMATCH,
                        $"Member {memberType} {memberInfo.Name} is not of type {AssemblyQualifiedTypeNames[i]}");
                    return false;
                }

                type = memberType;
            }

            if (hasPathChanged)
            {
                var serializedObject = new SerializedObject(context);
                EditorUtility.SetDirty(context);
                serializedObject.ApplyModifiedProperties();
            }
            
            errorCtx = BindingExpressionErrorContext.OK;
            return true;

        }
        #endif

        public void Initialize(GameObject gameObject)
        {
            switch (location)
            {
                case BindingExpressionLocation.InGameObject:
                    component = gameObject.GetComponent(Type.GetType(assemblyQualifiedTypeNames[0]));
                    break;
            }
        }

    }
}
