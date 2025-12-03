using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    public class NullBindingListener : BindingListenerBase
    {
        [SerializeField]
        private bool invert;
        
        [Header("Events")]
        [SerializeField]
        private UnityEvent nullEvent;

        [SerializeField]
        private UnityEvent notNullEvent;

        [Header("Set Active")]
        [SerializeField]
        private bool changesSetActive;

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            var go = gameObject;

            var value = GetExpression.Invoke(obj, go);

            var isNull = value == default;

            if (invert)
                isNull = !isNull;

            if (changesSetActive)
                go.SetActive(isNull);

            if (isNull)
                nullEvent.Invoke();
            else
                notNullEvent.Invoke();
        }
    }
}
