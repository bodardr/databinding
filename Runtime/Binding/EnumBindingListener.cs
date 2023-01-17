using UnityEngine;
#if UNITY_EDITOR
#endif


namespace Bodardr.Databinding.Runtime
{
    public class EnumBindingListener : BindingListener
    {
        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject[] values;

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

            var enumIndex = (int)GetExpression.Expression(obj);
            SetExpression.Expression(component, values[enumIndex].Value);
        }
    }
}