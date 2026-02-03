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
            out BindingExpressionErrorContext errorContext)
        {
            //Not enough TypeNames. Expression isn't filled.
            if (AssemblyQualifiedTypeNames.Length < 1)
            {
                errorContext = new BindingExpressionErrorContext(
                    BindingExpressionErrorContext.ErrorType.EMPTY_EXPRESSION,
                    "The expression is empty");
                return false;
            }

            errorContext = BindingExpressionPathValidator.TryFixingPath(context, this);

            if (errorContext.Error != BindingExpressionErrorContext.ErrorType.OK)
                return false;

            var type = Type.GetType(AssemblyQualifiedTypeNames[0]);
            if (location == BindingExpressionLocation.InGameObject)
            {
                //todo verify this.
                if (!typeof(Component).IsAssignableFrom(type))
                {
                    errorContext = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.HIERARCHY_TYPE_MISMATCH,
                        $"Expression targets GameObject, but the Input type {type.Name} is not a Component.");
                    return false;
                }

                if (!context.TryGetComponent(type, out _))
                {
                    errorContext = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.NO_MATCHING_COMPONENT_IN_GAMEOBJECT,
                        $"GameObject {context.name} has no component of type {type.Name}");
                    return false;
                }
            }
            else if (location == BindingExpressionLocation.InBindingNode)
            {
                if (bindingNode != null && type != bindingNode.BindingType)
                {
                    errorContext = new BindingExpressionErrorContext(
                        BindingExpressionErrorContext.ErrorType.BINDING_NODE_TYPE_MISMATCH,
                        $"Binding node is of type {(bindingNode.BindingType == null ? "null" : $"{bindingNode.BindingType.Name}")} but the path's input type is {type.Name}");
                    return false;
                }
            }

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
