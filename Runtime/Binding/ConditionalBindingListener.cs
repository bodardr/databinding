using UnityEngine;
using UnityEngine.XR;

namespace Bodardr.Databinding.Runtime
{
    public class ConditionalBindingListener : BindingListener
    {
        [SerializeField]
        private bool invert;

        [SerializeField] 
        private bool doNothingOnNull;
        
        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject trueValue;

        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject falseValue;

        public GenericSerializedObject TrueValue => trueValue;
        public GenericSerializedObject FalseValue => falseValue;

        public override void UpdateBinding(object obj)
        {
            if (!initialized)
                Awake();
            
            if (bindingNode == null &&
                bindingNodeSearchStrategy is NodeSearchStrategy.FindInParent or NodeSearchStrategy.FindInParentOfType)
            {
                bindingNode = GetBindingNodeInParent();
                if(bindingNode != null)
                    GetExpression.Subscribe(this, bindingNode);
            }
            
            var go = gameObject;
            var fetchedValue = GetExpression.Invoke(obj, go);
            var isTrue = false;
            
            if (fetchedValue != null)
                isTrue = (bool)fetchedValue;
            else if (doNothingOnNull)
                return;

            if (invert)
                isTrue = !isTrue;
            
            SetExpression.Invoke(obj, isTrue ? trueValue.Value : falseValue.Value, go);
        }
    }
}
