using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("bindingBehavior")]
        [SerializeField]
        [ShowIfEnum(nameof(searchStrategy), (int)SearchStrategy.SpecifyReference)]
        protected BindingNode bindingNode;

        private bool initialized = false;

        protected virtual void Awake()
        {
            if (!bindingNode && searchStrategy == SearchStrategy.FindInParent)
                bindingNode = gameObject.GetComponentInParent<BindingNode>(true);
            initialized = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingNode = gameObject.GetComponentInParent<BindingNode>(true);
        }

        public abstract void QueryExpressions(Dictionary<string, Tuple<BindingGetExpression, GameObject>> getExpressions, Dictionary<string, Tuple<BindingSetExpression,GameObject>> setExpressions);
#endif

        public virtual void OnBindingUpdated(object obj)
        {
            CheckForInitialization();
        }

        protected void CheckForInitialization()
        {
            if (!initialized)
                Awake();
        }
    }
}