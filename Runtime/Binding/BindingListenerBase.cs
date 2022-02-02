using System;
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

        private void OnValidate()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>(true);
        }

        public virtual void InitializeAndCompile()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>(true);
        }
        public abstract void UpdateValue(object obj);
    }
}