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
        public override void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            base.ValidateExpressions(errors);

            if (!SetExpression.IsValid(gameObject, bindingNode, out var setErr))
                errors.Add(new(gameObject, setErr, GetExpression));
        }
#endif

        public override void QueryExpressions(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> expressions,
            bool fromAoT)
        {
            base.QueryExpressions(expressions, fromAoT);

            var setExprType = typeof(BindingSetExpression);
            if (SetExpression.ShouldCompile(expressions, fromAoT))
                expressions[setExprType].Add(SetExpression.Path, new(SetExpression, gameObject));
        }

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
