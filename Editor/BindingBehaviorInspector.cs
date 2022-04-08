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

            var obj = (BindingBehavior)target;
            var objectTypeName = serializedObject.FindProperty("boundObjectTypeName");
            var type = Type.GetType(objectTypeName.stringValue);

            if (string.IsNullOrEmpty(objectTypeName.stringValue))
            {
                EditorGUILayout.LabelField("No type selected. Define a type below.", SearchWindowsCommon.errorStyle);
            }
            else
            {
                var bindingMethodProp = serializedObject.FindProperty("bindingMethod");
                switch ((BindingBehavior.BindingMethod)bindingMethodProp.enumValueIndex)
                {
                    case BindingBehavior.BindingMethod.Dynamic:
                        EditorGUILayout.LabelField("<color=green>Bound Dynamically</color>", boldLabel);

                        //Checks if the object can be auto-assigned
                        var isMono = typeof(MonoBehaviour).IsAssignableFrom(obj.BoundObjectType) && obj.GetComponent(obj.BoundObjectType) != null;

                        var canBeAutoAssignedProp = serializedObject.FindProperty("canBeAutoAssigned");
                        if (canBeAutoAssignedProp.boolValue != isMono)
                        {
                            canBeAutoAssignedProp.boolValue = isMono;
                            serializedObject.ApplyModifiedProperties();
                        }

                        if (isMono)
                        {
                            EditorGUILayout.LabelField(
                                "<color=cyan>Component</color> found, can be assigned automatically", boldLabel);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAssign"));
                            EditorGUILayout.Space();
                        }

                        break;

                    case BindingBehavior.BindingMethod.Manual:
                        EditorGUILayout.LabelField("<color=purple>Bound Manually.</color>", boldLabel);
                        EditorGUILayout.LabelField("Note : Must be updated manually.");
                        break;
                    case BindingBehavior.BindingMethod.Static:
                        EditorGUILayout.LabelField("<color=cyan>Bound Statically.</color>", boldLabel);
                        EditorGUILayout.LabelField(
                            "Note : Static class must implement event : OnPropertyChanged(string propertyName).");
                        break;
                }

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