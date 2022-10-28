using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bodardr.Databinding.Runtime
{
    public class FormattedBindingListener : BindingListener
    {
        [Tooltip("These getters start from index {1} inside the string format, onward")]
        [SerializeField]
        private List<BindingGetExpression> additionalGetters = new();

        [SerializeField]
        private string format;

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

#if UNITY_EDITOR
        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Formatted Listener")]
        public static void AddFormattedTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<FormattedBindingListener>();
            bindingListener.SetExpression.Path = "TextMeshProUGUI.text";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(TextMeshProUGUI).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(string).AssemblyQualifiedName;
        }
#endif
    }
}