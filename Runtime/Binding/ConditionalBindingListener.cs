﻿using UnityEngine;

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

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();
            
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
