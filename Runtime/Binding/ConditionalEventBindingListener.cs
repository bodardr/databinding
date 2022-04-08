using System;
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
        
        public override void InitializeAndCompile()
        {
            base.InitializeAndCompile();
            GetExpression.Compile();
        }

        private void Awake()
        {
            if (bindingBehavior == null)
            {
                Debug.LogWarning(
                    "Binding Behavior cannot be found, try changing Search Strategy or specify it manually.");
                return;
            }

            var active = gameObject.activeSelf;
            gameObject.SetActive(true);
            
            GetExpression.ResolveExpression();

            gameObject.SetActive(active);
            bindingBehavior.AddListener(this, GetExpression.Path);
        }

        public override void UpdateValue(object obj)
        {
            try
            {
                var fetchedValue = (bool)GetExpression.Expression(obj);

                if (invert)
                    fetchedValue = !fetchedValue;

                if(fetchedValue)
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