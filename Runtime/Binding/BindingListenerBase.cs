using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public enum SearchStrategy
    {
        FindInParent,
        SpecifyReference
    }

    public abstract class BindingListenerBase : MonoBehaviour
    {
        [SerializeField]
        protected SearchStrategy searchStrategy;

        [SerializeField]
        [ShowIfEnum(nameof(searchStrategy), (int)SearchStrategy.SpecifyReference)]
        protected BindingBehavior bindingBehavior;

        private bool initialized = false;

        protected virtual void Awake()
        {
            if (!bindingBehavior && searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>(true);
            initialized = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>(true);
        }
#endif

        public abstract void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions);

        public abstract void OnBindingUpdated(object obj);

        protected void CheckForInitialization()
        {
            if (!initialized)
                Awake();
        }
    }
}