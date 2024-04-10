using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Listener")]
    public class BindingListener : BindingListenerBase
    {
        private bool expressionsQueried = false;

        [SerializeField]
        protected BindingGetExpression getExpression = new();

        [SerializeField]
        protected BindingSetExpression setExpression = new();

        protected Component component;

        public BindingGetExpression GetExpression
        {
            get => getExpression;
            set => getExpression = value;
        }

        public BindingSetExpression SetExpression
        {
            get => setExpression;
            set => setExpression = value;
        }

        protected override void Awake()
        {
            base.Awake();

            component = GetComponent(Type.GetType(SetExpression.AssemblyQualifiedTypeNames[0]));

            var go = gameObject;

#if UNITY_EDITOR
            if (!expressionsQueried)
            {
                GetExpression.ResolveExpression(go);
                SetExpression.ResolveExpression(go);
            }
  #endif

            bindingNode.AddListener(this, GetExpression.Path);
        }


#if UNITY_EDITOR
        public override void QueryExpressions(Dictionary<string, Tuple<BindingGetExpression, GameObject>> getExpressions, Dictionary<string, Tuple<BindingSetExpression,GameObject>> setExpressions)
        {
            var go = gameObject;

            if (!GetExpression.ExpressionAlreadyCompiled && !getExpressions.ContainsKey(GetExpression.Path))
                getExpressions.Add(GetExpression.Path, new(GetExpression, go));

            if (!SetExpression.ExpressionAlreadyCompiled && !setExpressions.ContainsKey(SetExpression.Path))
                setExpressions.Add(SetExpression.Path, new(SetExpression, go));

            expressionsQueried = true;
        }
  #endif

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            try
            {
                var fetchedValue = GetExpression.Expression(obj);
                SetExpression.Expression(component, fetchedValue);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"<b><color=red>Error with expressions {GetExpression.Path} / {SetExpression.Path} in {gameObject.name}</color></b> {e}",
                    this);
            }
        }
    }
}