using UnityEngine;

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

            var go = gameObject;
            var enumValue = GetExpression.Invoke(obj, go);
            if (enumValue != null)
                SetExpression.Invoke(obj, values[(int)enumValue].Value, go);
        }
    }
}
