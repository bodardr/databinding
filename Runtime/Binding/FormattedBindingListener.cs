using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    public class FormattedBindingListener : BindingListener
    {
        [Tooltip("These getters start from index {1} inside the string format, onward")]
        [SerializeField]
        private List<BindingGetExpression> additionalGetters = new();

        [SerializeField]
        private string format;

        [SerializeField]
        private bool getterExpressionIsNumeric;

        [SerializeField]
        private bool convertFloatToTimeSpan;

        protected override void Awake()
        {
            //Additional getters must be resolved before calling the BindingListener's Awake
            //Because once the base listener awakes, it binds itself to the binding behavior.
            var go = gameObject;
            foreach (var getter in additionalGetters)
                getter.ResolveExpression(go);

            base.Awake();
        }

        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            base.QueryExpressions(expressions);

            var go = gameObject;
            foreach (var expr in additionalGetters)
            {
                var path = expr.Path;
                if (!expr.ExpressionAlreadyCompiled && !expressions.ContainsKey(path))
                    expressions.Add(path, new(expr, go));
            }
        }

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

            var fetchedValue = GetExpression.Expression(obj);

            if (getterExpressionIsNumeric && convertFloatToTimeSpan)
                fetchedValue = TimeSpan.FromSeconds((double)fetchedValue);

            if (additionalGetters.Count > 0)
            {
                object[] values = new object[additionalGetters.Count + 1];
                values[0] = fetchedValue;
                for (var i = 0; i < additionalGetters.Count; i++)
                    values[i + 1] = additionalGetters[i].Expression(obj);

                SetExpression.Expression(component, string.Format(format, values));
            }
            else
            {
                SetExpression.Expression(component, string.Format(format, fetchedValue ?? string.Empty));
            }
        }
    }
}