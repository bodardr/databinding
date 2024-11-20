using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    public class ConditionalEventBindingListener : BindingListenerBase
    {
        [SerializeField]
        private bool invert;

        [SerializeField] private bool doNothingOnNull;

        [SerializeField]
        private UnityEvent onValueTrue;

        [SerializeField]
        private UnityEvent onValueFalse;

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            var fetchedValue = GetExpression.Invoke(obj, gameObject);
            bool isTrue = false;

            if (fetchedValue != null)
                isTrue = (bool)fetchedValue;
            else if (doNothingOnNull)
                return;

            if (invert)
                isTrue = !isTrue;

            if (isTrue)
                onValueTrue.Invoke();
            else
                onValueFalse.Invoke();
        }
    }
}
