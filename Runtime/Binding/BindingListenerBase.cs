using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bodardr.Databinding.Runtime
{
    public enum NodeSearchStrategy
    {
        FindInParent,
        SpecifyReference,
    }

    public enum UpdateMethod
    {
        Automatic,
        OnUpdate,
        Periodical,
    }

    public enum ListenerSubscribeMethod
    {
        EnableAndDisable,
        AwakeAndDestroy
    }

    public abstract class BindingListenerBase : MonoBehaviour
    {
        private bool initialized = false;

        [SerializeField]
        private ListenerSubscribeMethod bindingNodeSubscriptionMethod = ListenerSubscribeMethod.EnableAndDisable;

        [SerializeField]
        protected NodeSearchStrategy bindingNodeSearchStrategy;

        [SerializeField]
        protected UpdateMethod updateMethod;

        [SerializeField]
        [ShowIfEnum(nameof(updateMethod), (int)UpdateMethod.Periodical)]
        [Min(0.01f)]
        protected float updateInterval;

        [SerializeField]
        [ShowIfEnum(nameof(bindingNodeSearchStrategy), (int)NodeSearchStrategy.SpecifyReference)]
        protected BindingNode bindingNode;

        [SerializeField]
        private bool updateOnEnable = true;

        [Space]
        [SerializeField]
        protected BindingGetExpression getExpression = new();

        public BindingGetExpression GetExpression
        {
            get => getExpression;
            set => getExpression = value;
        }

#if !ENABLE_IL2CPP || UNITY_EDITOR
        public virtual void QueryExpressions(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> expressions,
            bool fromAoT = false)
        {
            var getExprType = typeof(BindingGetExpression);
            if (GetExpression.ShouldCompile(expressions, fromAoT))
                expressions[getExprType].Add(GetExpression.Path, new(GetExpression, gameObject));
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bindingNodeSearchStrategy == NodeSearchStrategy.FindInParent)
                bindingNode = GetComponentInParent<BindingNode>(true);
        }


        public virtual void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            if (!gameObject.scene.IsValid())
                return;

            if (!GetExpression.IsValid(gameObject, bindingNode, out var getErr))
                errors.Add(new(gameObject, getErr, GetExpression));
        }
#endif

        protected virtual void Awake()
        {
            if (bindingNode == null && bindingNodeSearchStrategy == NodeSearchStrategy.FindInParent)
                bindingNode = gameObject.GetComponentInParent<BindingNode>(true);

            GetExpression.Initialize(gameObject);

            if (bindingNodeSubscriptionMethod == ListenerSubscribeMethod.AwakeAndDestroy)
                GetExpression.Subscribe(this, bindingNode);

            initialized = true;
        }

        protected virtual void OnEnable()
        {
            if (bindingNodeSubscriptionMethod == ListenerSubscribeMethod.EnableAndDisable)
                GetExpression.Subscribe(this, bindingNode);

            if (updateOnEnable)
                OnBindingUpdated(bindingNode != null ? bindingNode.Binding : null);

            if (updateMethod == UpdateMethod.Periodical)
                StartCoroutine(PeriodicalUpdateCoroutine());
        }
        
        protected virtual void Update()
        {
            if (updateMethod == UpdateMethod.OnUpdate)
                OnBindingUpdated(bindingNode != null ? bindingNode.Binding : null);
        }

        protected virtual void OnDisable()
        {
            if (bindingNodeSubscriptionMethod == ListenerSubscribeMethod.EnableAndDisable)
                GetExpression.Unsubscribe(this, bindingNode);
        }

        private void OnDestroy()
        {
            if (bindingNodeSubscriptionMethod == ListenerSubscribeMethod.AwakeAndDestroy)
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

        private IEnumerator PeriodicalUpdateCoroutine()
        {
            while (updateMethod == UpdateMethod.Periodical && isActiveAndEnabled)
            {
                OnBindingUpdated(bindingNode != null ? bindingNode.Binding : null);
                yield return WaitForSecondsPool.Get(updateInterval);
            }
        }
    }
}
