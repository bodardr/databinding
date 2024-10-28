using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(ConditionalEventBindingListener), false)]
    public class ConditionalEventBindingListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bindingListener = (ConditionalEventBindingListener)target;
            Type getterMemberType = null;

            if (!string.IsNullOrEmpty(bindingListener.GetExpression.Path) &&
                !string.IsNullOrEmpty(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]))
                getterMemberType = Type.GetType(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]);

            var isValid = getterMemberType == typeof(bool);

            if (!isValid)
                EditorGUILayout.LabelField("<b>Note:</b> Property is not a bool.", SearchWindowsCommon.errorStyle);

            base.OnInspectorGUI();
        }
    }
}
