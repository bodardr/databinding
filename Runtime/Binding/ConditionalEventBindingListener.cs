﻿using System;
using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    public class ConditionalEventBindingListener : BindingListenerBase
    {
        [SerializeField]
        private bool invert;

        [SerializeField]
        private UnityEvent onValueTrue;

        [SerializeField]
        private UnityEvent onValueFalse;

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);
            
            var fetchedValue = (bool)GetExpression.Invoke(obj, gameObject);

            if (invert)
                fetchedValue = !fetchedValue;

            if (fetchedValue)
                onValueTrue.Invoke();
            else
                onValueFalse.Invoke();
        }
    }
}
