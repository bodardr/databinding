using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bodardr.Databinding.Runtime
{
    public class EnumBindingListener : BindingListener
    {
        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject[] values;

        public override void UpdateBinding(object obj)
        {
            CheckForInitialization();

            var go = gameObject;
            var enumValue = GetExpression.Invoke(obj, go);
            if (enumValue != null)
                SetExpression.Invoke(obj, values[(int)enumValue].Value, go);
        }

        #if UNITY_EDITOR
        public override void ValidateExpressions(
            List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
        {
            base.ValidateExpressions(errors);

            var enumValues = Enum.GetValues(Type.GetType(GetExpression.AssemblyQualifiedTypeNames[^1]));

            if (values.Length == enumValues.Length)
                return;
            
            Array.Resize(ref values, enumValues.Length);
            EditorUtility.SetDirty(this);
            using var serializedObject = new SerializedObject(this);
            serializedObject.ApplyModifiedProperties();
        }
        #endif
    }
}
