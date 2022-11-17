using System;
using System.Collections.Generic;
using System.Linq;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
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
            protected set => getExpression = value;
        }

        public BindingSetExpression SetExpression
        {
            get => setExpression;
            protected set => setExpression = value;
        }

        protected override void Awake()
        {
            base.Awake();

            component = GetComponent(Type.GetType(SetExpression.AssemblyQualifiedTypeNames[0]));

            var go = gameObject;

            if (!expressionsQueried)
            {
                GetExpression.ResolveExpression(go);
                SetExpression.ResolveExpression(go);
            }

            bindingBehavior.AddListener(this, GetExpression.Path);
        }

        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            var go = gameObject;

            if (!GetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(GetExpression.Path))
                expressions.Add(GetExpression.Path, new(GetExpression, go));

            if (!SetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(SetExpression.Path))
                expressions.Add(SetExpression.Path, new(SetExpression, go));

            expressionsQueried = true;
        }

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

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