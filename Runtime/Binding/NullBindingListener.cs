using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    public class NullBindingListener : BindingListenerBase
    {
        [SerializeField]
        private bool invert;

        [SerializeField]
        private BindingSetExpression setExpression;

        [SerializeField]
        private GenericSerializedObject nullValue;

        [SerializeField]
        private GenericSerializedObject notNullValue;

        [Header("Events")]
        [SerializeField]
        private UnityEvent nullEvent;

        [SerializeField]
        private UnityEvent notNullEvent;

        [Header("Set Active")]
        [SerializeField]
        private bool changesSetActive;

        private Component component;

        public BindingSetExpression SetExpression => setExpression;

        protected override void Awake()
        {
            if (!string.IsNullOrEmpty(setExpression.Path))
                component = GetComponent(Type.GetType(setExpression.AssemblyQualifiedTypeNames[0]));

            bindingBehavior.AddListener(this);
            SetExpression.ResolveExpression(gameObject);
        }

        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            if (!SetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(SetExpression.Path))
                expressions.Add(SetExpression.Path, new(SetExpression, gameObject));
        }

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

            var isNull = obj == null;

            if (invert)
                isNull = !isNull;

            if (changesSetActive)
                gameObject.SetActive(isNull);

            if (isNull)
                nullEvent.Invoke();
            else
                notNullEvent.Invoke();

            SetExpression.Expression?.Invoke(component, isNull ? nullValue.Value : notNullValue.Value);
        }
    }
}