using System;
using System.Collections.Generic;
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

            bindingNode.AddListener(this);

            #if UNITY_EDITOR
            SetExpression.ResolveExpression(gameObject);
            #endif
        }

        #if UNITY_EDITOR
        public override void QueryExpressions(Dictionary<string, Tuple<BindingGetExpression, GameObject>> getExpressions, Dictionary<string, Tuple<BindingSetExpression, GameObject>> setExpressions)
        {
            if (!SetExpression.ExpressionAlreadyCompiled && !setExpressions.ContainsKey(SetExpression.Path))
                setExpressions.Add(SetExpression.Path, new(SetExpression, gameObject));
        }
        #endif

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

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
