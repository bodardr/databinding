using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(EnumBindingListener), true)]
    public class EnumBindingListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bindingListener = (BindingListener)target;
            Type getterMemberType = null;

            if (!string.IsNullOrEmpty(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]))
                getterMemberType = Type.GetType(bindingListener.GetExpression.AssemblyQualifiedTypeNames[^1]);

            var isValid = typeof(Enum).IsAssignableFrom(getterMemberType);

            if (!isValid)
                EditorGUILayout.LabelField("<b>Note:</b> Property is not an enum.", SearchWindowsCommon.errorStyle);

            base.OnInspectorGUI();

            if (!isValid)
                return;

            Type setterMemberType = null;
            if (!string.IsNullOrEmpty(bindingListener.SetExpression.AssemblyQualifiedTypeNames[^1]))
                setterMemberType = Type.GetType(bindingListener.SetExpression.AssemblyQualifiedTypeNames[^1]);

            var enumNames = Enum.GetNames(getterMemberType);

            var valueArray = serializedObject.FindProperty("values");

            if (valueArray.arraySize != enumNames.Length)
            {
                valueArray.arraySize = enumNames.Length;
                serializedObject.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(serializedObject.targetObject);
            }

            for (int i = 0; i < valueArray.minArraySize; i++)
            {
                var value = valueArray.GetArrayElementAtIndex(i);
                value.DrawGenericSerializedObject(setterMemberType, enumNames[i]);
            }
        }
    }
}