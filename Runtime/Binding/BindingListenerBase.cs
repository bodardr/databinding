using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public enum NodeSearchStrategy
    {
        FindInParent,
        SpecifyReference,
    }

    public abstract class BindingListenerBase : MonoBehaviour
    {
        private bool initialized = false;

        [SerializeField]
        protected NodeSearchStrategy bindingNodeSearchStrategy;

        [SerializeField]
        [ShowIfEnum(nameof(bindingNodeSearchStrategy), (int)NodeSearchStrategy.SpecifyReference)]
        protected BindingNode bindingNode;

        [SerializeField]
        protected BindingGetExpression getExpression = new();

        public BindingGetExpression GetExpression
        {
            get => getExpression;
            set => getExpression = value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bindingNodeSearchStrategy == NodeSearchStrategy.FindInParent)
                bindingNode = GetComponentInParent<BindingNode>(true);
        }

        public virtual void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            if (!expressions.ContainsKey(GetExpression.Path))
                expressions.Add(GetExpression.Path, new(GetExpression, gameObject));
        }

        public virtual void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            if (!GetExpression.IsValid(gameObject, bindingNode, out var getErr))
                errors.Add(new(gameObject, getErr, GetExpression));
        }
#endif


        protected virtual void Awake()
        {
            if (bindingNode == null && bindingNodeSearchStrategy == NodeSearchStrategy.FindInParent)
                bindingNode = gameObject.GetComponentInParent<BindingNode>(true);
            
            GetExpression.Initialize(gameObject);

            initialized = true;
        }
        
        protected virtual void OnEnable()
        {
            GetExpression.Subscribe(this, bindingNode);
            OnBindingUpdated(bindingNode.Binding);
        }

        protected virtual void OnDisable()
        {
            GetExpression.Unsubscribe(this, bindingNode);
        }

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
