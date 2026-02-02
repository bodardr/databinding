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

#if !ENABLE_IL2CPP || UNITY_EDITOR
        public override void QueryExpressions(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> expressions,
            bool fromAoT)
        {
            base.QueryExpressions(expressions, fromAoT);

            var setExprType = typeof(BindingSetExpression);
            if (SetExpression.ShouldCompile(expressions, fromAoT))
                expressions[setExprType].Add(SetExpression.Path, new(SetExpression, gameObject));
        }
#endif

#if UNITY_EDITOR
        public override void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            base.ValidateExpressions(errors);

            if (!SetExpression.IsValid(this, bindingNode, out var setErr))
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
