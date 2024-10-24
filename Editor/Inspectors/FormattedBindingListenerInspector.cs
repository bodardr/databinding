using System;
using System.Linq;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(FormattedBindingListener), false)]
    public class FormattedBindingListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var formattedBindingListener = (FormattedBindingListener)target;

            var isGetterDefined = formattedBindingListener.GetExpression.AssemblyQualifiedTypeNames.Length > 0;

            var lastType = isGetterDefined ?
                Type.GetType(formattedBindingListener.GetExpression.AssemblyQualifiedTypeNames.Last()) :
                null;

            var getterExpressionIsNumeric =
                formattedBindingListener.GetExpression != null &&
                isGetterDefined &&
                lastType.IsNumericType();

            serializedObject.FindProperty(nameof(getterExpressionIsNumeric)).boolValue = getterExpressionIsNumeric;

            if (getterExpressionIsNumeric)
            {
                EditorStyles.label.richText = true;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("convertGetterToTimeSpan"),
                    new GUIContent(
                        $"Convert <b><color=yellow>{lastType.Name}</color></b> to <color=cyan><b>TimeSpan</b></color>?"));
                EditorStyles.label.richText = false;
            }

            //todo : Add preview functionality.

            serializedObject.ApplyModifiedProperties();
        }
    }
}
