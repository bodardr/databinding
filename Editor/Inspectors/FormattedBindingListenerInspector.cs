using System;
using System.Linq;
using Bodardr.Databinding.Runtime;
using Bodardr.Utility.Editor.Editor.Scripts;
using Bodardr.Utility.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(FormattedBindingListener))]
    public class FormattedBindingListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var formattedBindingListener = (FormattedBindingListener)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchStrategy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bindingNode"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("getExpression"));

            var getterExpressionIsNumeric = formattedBindingListener.GetExpression != null &&
                                            Type.GetType(formattedBindingListener.GetExpression
                                                .AssemblyQualifiedTypeNames.Last()).IsNumericType();
            serializedObject.FindProperty(nameof(getterExpressionIsNumeric)).boolValue = getterExpressionIsNumeric;

            if (getterExpressionIsNumeric)
            {
                var originalLabel = EditorStyles.label;

                var labelWithRichText = EditorStyles.label;
                labelWithRichText.richText = true;

                EditorStyles.label.Assign(labelWithRichText);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("convertGetterToTimeSpan"),
                    new GUIContent("Convert getter to <color=cyan><b>TimeSpan</b></color>?"));

                EditorStyles.label.Assign(originalLabel);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("setExpression"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("additionalGetters"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("format"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}