using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(BindingBehavior))]
    [CanEditMultipleObjects]
    public class BindingBehaviorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var boldLabel = new GUIStyle(EditorStyles.boldLabel);
            boldLabel.richText = true;

            var label = new GUIStyle(EditorStyles.label);
            label.richText = true;

            EditorGUILayout.Space();

            var objectTypeName = serializedObject.FindProperty("boundObjectTypeName");
            var type = Type.GetType(objectTypeName.stringValue);

            if (string.IsNullOrEmpty(objectTypeName.stringValue))
            {
                EditorGUILayout.LabelField("No type selected. Define a type below.", SearchWindowsCommon.noResultStyle);
            }
            else
            {
                if (type?.GetInterface("INotifyPropertyChanged") != null)
                {
                    EditorGUILayout.LabelField("<color=green>Bound Dynamically</color>", boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("<color=cyan>Bound Statically.</color>", boldLabel);
                    EditorGUILayout.LabelField("Note : Must be updated manually.");
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Type : <b>{type?.FullName}</b>", label);
            }

            if (GUILayout.Button("Bound Object Type"))
            {
                BoundTypeSearchWindow.Popup(type?.FullName, SetBoundObjectType);
                EditorGUILayout.Space();
            }
        }

        private void SetBoundObjectType(string value)
        {
            serializedObject.FindProperty("boundObjectTypeName").stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}