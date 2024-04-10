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

            if (!bindingNode)
            {
                Debug.LogWarning(
                    "Binding Behavior cannot be found, try changing Search Strategy or specify it manually.");
                return;
            }

            #if UNITY_EDITOR
            GetExpression.ResolveExpression(gameObject);
            #endif
            bindingNode.AddListener(this, GetExpression.Path);
        }

#if UNITY_EDITOR
        public override void QueryExpressions(Dictionary<string, Tuple<BindingGetExpression, GameObject>> getExpressions, Dictionary<string, Tuple<BindingSetExpression, GameObject>> setExpressions)
        {
            if (!GetExpression.ExpressionAlreadyCompiled && !getExpressions.ContainsKey(GetExpression.Path))
                getExpressions.Add(GetExpression.Path, new(GetExpression, gameObject));
        }
  #endif

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

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
            catch(Exception)
            {
                Debug.LogError(
                    $"<b><color=red>Error with expression {GetExpression.Path} in {gameObject.name}</color></b>");
            }
        }
    }
}
