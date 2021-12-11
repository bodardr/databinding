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

        public override void InitializeAndCompile()
        {
            base.InitializeAndCompile();
            
            foreach (var getter in additionalGetters)
                getter.Compile();
        }

        protected override void Awake()
        {
            base.Awake();

            foreach (var getter in additionalGetters)
                getter.ResolveExpression();
        }

        public override void UpdateValue(object obj)
        {
            var fetchedValue = GetExpression.Expression(obj);

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