using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    public class ConditionalEventBindingListener : BindingListenerBase
    {
        [SerializeField]
        protected BindingGetExpression getExpression = new();

        [SerializeField]
        private bool invert;

        [SerializeField]
        private UnityEvent onValueTrue;

        [SerializeField]
        private UnityEvent onValueFalse;

        public BindingGetExpression GetExpression => getExpression;

        protected override void Awake()
        {
            base.Awake();

            if (!bindingBehavior)
            {
                Debug.LogWarning(
                    "Binding Behavior cannot be found, try changing Search Strategy or specify it manually.");
                return;
            }

            GetExpression.ResolveExpression(gameObject);
            bindingBehavior.AddListener(this, GetExpression.Path);
        }

        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            if (!GetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(GetExpression.Path))
                expressions.Add(GetExpression.Path, new(GetExpression, gameObject));
        }

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

            try
            {
                var fetchedValue = (bool)GetExpression.Expression(obj);

                if (invert)
                    fetchedValue = !fetchedValue;

                if (fetchedValue)
                    onValueTrue.Invoke();
                else
                    onValueFalse.Invoke();
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"<b><color=red>Error with expression {GetExpression.Path} in {gameObject.name}</color></b>");
            }
        }
    }
}