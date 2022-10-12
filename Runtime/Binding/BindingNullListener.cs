using System;
using System.Collections;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
using UnityEngine.Events;

namespace Bodardr.Databinding.Runtime
{
    [Obsolete]
    public class BindingNullListener : BindingListenerBase
    {
        /// <summary>
        /// Determines if this component is synchronized with another BindingListener.
        /// </summary>
        [SerializeField]
        private bool bindingListenerSynchro = false;

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

        private void Awake()
        {
            var active = gameObject.activeSelf;

            gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(setExpression.Path))
                component = GetComponent(Type.GetType(setExpression.AssemblyQualifiedTypeNames[0]));
            gameObject.SetActive(active);

            bindingBehavior.AddListener(this);
            SetExpression.ResolveExpression();
        }

        private void Start()
        {
            //If another BindingComponent is present
            if (GetComponent<BindingListener>() != null)
                bindingListenerSynchro = true;

            StartCoroutine(ValueCheckCoroutine());
        }

        public override void InitializeAndCompile()
        {
            base.InitializeAndCompile();

            if (string.IsNullOrEmpty(SetExpression.Path))
                return;

            SetExpression.Compile();
        }

        private IEnumerator ValueCheckCoroutine()
        {
            yield return new WaitForEndOfFrame();

            if (!bindingBehavior.IsObjectSet)
                UpdateValue(null);
        }

        public override void UpdateValue(object obj)
        {
            var isNull = obj == null;

            if (invert)
                isNull = !isNull;

            if (changesSetActive)
                gameObject.SetActive(isNull);

            if (isNull)
                nullEvent.Invoke();
            else if (!bindingListenerSynchro)
                notNullEvent.Invoke();

            if (SetExpression.Expression == null)
                return;

            if (isNull)
                SetExpression.Expression(component, nullValue.Value);
            else if (!bindingListenerSynchro)
                SetExpression.Expression(component, notNullValue.Value);
        }
    }
}