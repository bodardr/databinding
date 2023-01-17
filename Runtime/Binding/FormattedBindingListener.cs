using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
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

        [SerializeField]
        private bool getterExpressionIsNumeric;

        [SerializeField]
        private bool convertGetterToTimeSpan;

        protected override void Awake()
        {
            //Additional getters must be resolved before calling the BindingListener's Awake
            //Because once the base listener awakes, it binds itself to the binding behavior.
            var go = gameObject;
            foreach (var getter in additionalGetters)
                getter.ResolveExpression(go);

            if (getterExpressionIsNumeric && convertGetterToTimeSpan)
                getterTypeCode = Type.GetTypeCode(Type.GetType(GetExpression.AssemblyQualifiedTypeNames[^1]));

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

            if (getterExpressionIsNumeric && convertGetterToTimeSpan)
                fetchedValue = TimeSpan.FromSeconds(UnboxValueToDouble(fetchedValue, getterTypeCode));

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