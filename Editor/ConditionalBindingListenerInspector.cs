using System;
using Bodardr.Databinding.Runtime;
using NUnit.Compatibility;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(ConditionalBindingListener), true)]
    public class ConditionalBindingListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bindingListener = (BindingListener)target;
            Type getterMemberType = null;

            if (!string.IsNullOrEmpty(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]))
                getterMemberType = Type.GetType(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]);

            var isValid = typeof(bool).IsCastableFrom(getterMemberType);

            if (!isValid)
                EditorGUILayout.LabelField("<b>Note:</b> Property cannot be evaluated as a bool.", SearchWindowsCommon.errorStyle);

            base.OnInspectorGUI();

            if (!isValid)
                return;

            Type setterMemberType = null;

            if (!string.IsNullOrEmpty(bindingListener.SetExpression.AssemblyQualifiedTypeNames[^1]))
                setterMemberType = Type.GetType(bindingListener.SetExpression.AssemblyQualifiedTypeNames[^1]);

            var trueProp = serializedObject.FindProperty("trueValue");
            var falseProp = serializedObject.FindProperty("falseValue");

            trueProp.DrawGenericSerializedObject(setterMemberType);
            falseProp.DrawGenericSerializedObject(setterMemberType);
        }
    }
}