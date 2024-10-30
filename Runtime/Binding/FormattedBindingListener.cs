using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    public class FormattedBindingListener : BindingListener
    {
        private TypeCode getterTypeCode;

        [Tooltip("These getters start from index {1} inside the string format, onward")]
        [SerializeField]
        private List<BindingGetExpression> additionalGetters = new();

        [SerializeField]
        private string format;

        [HideInInspector]
        [SerializeField]
        private bool getterExpressionIsNumeric;

        [HideInInspector]
        [SerializeField]
        private bool convertGetterToTimeSpan;

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
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (var getter in additionalGetters)
                getter.Unsubscribe(this, bindingNode);
        }

        public override void OnBindingUpdated(object obj)
        {
            base.OnBindingUpdated(obj);

            var go = gameObject;

            var fetchedValue = GetExpression.Invoke(obj, go);

            if (getterExpressionIsNumeric && convertGetterToTimeSpan)
                fetchedValue = TimeSpan.FromSeconds(UnboxValueToDouble(fetchedValue, getterTypeCode));

            if (additionalGetters.Count > 0)
            {
                object[] values = new object[additionalGetters.Count + 1];
                values[0] = fetchedValue;
                for (var i = 0; i < additionalGetters.Count; i++)
                    values[i + 1] = additionalGetters[i].Invoke(obj, go);

                SetExpression.Invoke(obj, string.Format(format, values), go);
            }
            else
            {
                SetExpression.Invoke(obj, string.Format(format, fetchedValue ?? string.Empty), go);
            }
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
