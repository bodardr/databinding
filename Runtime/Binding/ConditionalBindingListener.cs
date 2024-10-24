using UnityEngine;

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
            CheckForInitialization();
            
            var go = gameObject;
            var fetchedValue = (bool)GetExpression.Invoke(obj, go);

            if (invert)
                fetchedValue = !fetchedValue;

            SetExpression.Invoke(obj, fetchedValue ? trueValue.Value : falseValue.Value, go);
        }
    }
}
