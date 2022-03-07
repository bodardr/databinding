using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;
#endif


namespace Bodardr.Databinding.Runtime
{
    public class ConditionalBindingListener : BindingListener
    {
        [SerializeField]
        private bool invert;

        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject trueValue;

        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject falseValue;

#if UNITY_EDITOR
        [MenuItem("CONTEXT/Image/Databinding - Add Conditional Listener")]
        public new static void AddImageListener(MenuCommand menuCommand)
        {
            var bindingListener =
                ((Component)menuCommand.context).gameObject.AddComponent<ConditionalBindingListener>();

            bindingListener.SetExpression.Path = "Image.sprite";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] = typeof(Image).AssemblyQualifiedName;
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] = typeof(Sprite).AssemblyQualifiedName;
        }
#endif

        public override void UpdateValue(object obj)
        {
            try
            {
                var fetchedValue = (bool)GetExpression.Expression(obj);

                if (invert)
                    fetchedValue = !fetchedValue;

                SetExpression.Expression(component, fetchedValue ? trueValue.Value : falseValue.Value);
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"<b><color=red>Error with expressions {GetExpression.Path} / {SetExpression.Path} in {gameObject.name}</color></b>");
            }
        }
    }
}