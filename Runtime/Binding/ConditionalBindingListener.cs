using System;
using UnityEngine;
#if UNITY_EDITOR
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

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);
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
                    $"<b><color=red>Error with expressions in {gameObject.name} : \n - {GetExpression.Path},\n - {SetExpression.Path}</color></b>");
            }
        }
    }
}