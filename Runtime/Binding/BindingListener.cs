using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Listener")]
    public class BindingListener : BindingListenerBase
    {
        [SerializeField]
        protected BindingSetExpression setExpression = new();

        public BindingSetExpression SetExpression
        {
            get => setExpression;
            set => setExpression = value;
        }

#if UNITY_EDITOR
        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            base.QueryExpressions(expressions);

            if (!expressions.ContainsKey(SetExpression.Path))
                expressions.Add(SetExpression.Path, new(SetExpression, gameObject));
        }

        public override void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            if (!SetExpression.IsValid(gameObject, bindingNode, out var setErr))
                errors.Add(new(gameObject, setErr, GetExpression));
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            SetExpression.Initialize(gameObject);
        }

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            var go = gameObject;
            var fetchedValue = GetExpression.Invoke(obj, go);
            SetExpression.Invoke(obj, fetchedValue, go);
        }
    }
}
