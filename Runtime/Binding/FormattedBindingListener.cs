using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

namespace Bodardr.Databinding.Runtime
{
    public class FormattedBindingListener : BindingListener
    {
        private TypeCode getterTypeCode;
        private object[] args = new object[1];

        [Tooltip("These getters start from index {1} inside the string format, onward")]
        [SerializeField]
        private List<BindingGetExpression> additionalGetters = new();

        #if UNITY_LOCALIZATION
        [SerializeField]
        private bool localize;

        [SerializeField]
        [ShowIf(nameof(localize))]
        private LocalizedString localizedString;
        #endif

        #if UNITY_LOCALIZATION
        [ShowIf(nameof(localize), true)]
        #endif
        [SerializeField]
        private string format;


        [HideInInspector]
        [SerializeField]
        private bool getterExpressionIsNumeric;

        [HideInInspector]
        [SerializeField]
        private bool convertGetterToTimeSpan;

#if !ENABLE_IL2CPP || UNITY_EDITOR
        public override void QueryExpressions(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> expressions,
            bool fromAoT)
        {
            base.QueryExpressions(expressions, fromAoT);

            var go = gameObject;
            var getExprType = typeof(BindingGetExpression);

            foreach (var expr in additionalGetters)
                if (expr.ShouldCompile(expressions, fromAoT))
                    expressions[getExprType].Add(expr.Path, new(expr, go));
        }
#endif

#if UNITY_EDITOR
        public override void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            base.ValidateExpressions(errors);

            var go = gameObject;
            foreach (var expr in additionalGetters)
                if (!expr.IsValid(go, bindingNode, out var errorCtx))
                    errors.Add(new(go, errorCtx, expr));
        }
#endif

        protected override void Awake()
        {
            if (getterExpressionIsNumeric && convertGetterToTimeSpan)
                getterTypeCode = Type.GetTypeCode(Type.GetType(GetExpression.AssemblyQualifiedTypeNames[^1]));

            base.Awake();

            foreach (var getter in additionalGetters)
                getter.Initialize(gameObject);
        }

        protected override void OnEnable()
        {
            foreach (var getter in additionalGetters)
                getter.Subscribe(this, bindingNode);

            base.OnEnable();

            #if UNITY_LOCALIZATION
            if (localize && localizedString != null)
                localizedString.StringChanged += BindingUpdatedFromLocalization;
            #endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (var getter in additionalGetters)
                getter.Unsubscribe(this, bindingNode);

            #if UNITY_LOCALIZATION
            if (localize && localizedString != null)
                localizedString.StringChanged -= BindingUpdatedFromLocalization;
            #endif
        }

        private void BindingUpdatedFromLocalization(string localizedString)
        {
            OnBindingUpdated(bindingNode != null ? bindingNode.Binding : null);
        }

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            var go = gameObject;

            //First argument.
            var fetchedValue = GetExpression.Invoke(obj, go);

            if (fetchedValue != null && getterExpressionIsNumeric && convertGetterToTimeSpan)
                fetchedValue = TimeSpan.FromSeconds(UnboxValueToDouble(fetchedValue, getterTypeCode));

            args[0] = fetchedValue;


            if (additionalGetters.Count > 0)
            {
                if (args.Length != additionalGetters.Count + 1)
                    args = new object[additionalGetters.Count + 1];

                args[0] = fetchedValue;
                for (var i = 0; i < additionalGetters.Count; i++)
                    args[i + 1] = additionalGetters[i].Invoke(obj, go);
            }

#if UNITY_LOCALIZATION
            if (localize)
                SetExpression.Invoke(obj, localizedString.GetLocalizedString(args), go);
            else
                SetExpression.Invoke(obj, string.Format(format, args), go);
#else            
            SetExpression.Invoke(obj, string.Format(format, args), go);
#endif
        }

        private double UnboxValueToDouble(object fetchedValue, TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Decimal:
                    return (double)(decimal)fetchedValue;
                case TypeCode.Double:
                    return (double)fetchedValue;
                case TypeCode.Int16:
                    return (short)fetchedValue;
                case TypeCode.Int32:
                    return (int)fetchedValue;
                case TypeCode.Int64:
                    return (long)fetchedValue;
                case TypeCode.Single:
                    return (float)fetchedValue;
                case TypeCode.UInt16:
                    return (ushort)fetchedValue;
                case TypeCode.UInt32:
                    return (uint)fetchedValue;
                case TypeCode.UInt64:
                    return (ulong)fetchedValue;
            }

            return default;
        }
    }
}
