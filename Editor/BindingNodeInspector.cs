using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(BindingNode))]
    [CanEditMultipleObjects]
    public class BindingNodeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var boldLabel = new GUIStyle(EditorStyles.boldLabel);
            boldLabel.richText = true;

            var label = new GUIStyle(EditorStyles.label);
            label.richText = true;

            EditorGUILayout.Space();

            var obj = (BindingNode)target;
            var objectTypeName = serializedObject.FindProperty("bindingTypeName");
            var type = Type.GetType(objectTypeName.stringValue);

            if (string.IsNullOrEmpty(objectTypeName.stringValue))
            {
                EditorGUILayout.LabelField("No type selected. Define a type below.", SearchWindowsCommon.errorStyle);
            }
            else
            {
                var bindingMethodProp = serializedObject.FindProperty("bindingMethod");
                switch ((BindingMethod)bindingMethodProp.enumValueIndex)
                {
                    case BindingMethod.Dynamic:
                        EditorGUILayout.LabelField("<color=#22e05b>Bound Dynamically</color>", boldLabel);
                        CheckAutoAssign(obj, "#22e05b");
                        break;

                    case BindingMethod.Manual:
                        EditorGUILayout.LabelField("<color=#514fc4>Bound Manually.</color>", boldLabel);
                        EditorGUILayout.LabelField("Note : Must be updated manually.");
                        CheckAutoAssign(obj, "#514fc4");

                        EditorGUILayout.Space();

                        var manualUpdateMethodProp = serializedObject.FindProperty("manualUpdateMethod");
                        manualUpdateMethodProp.enumValueIndex =
                            (int)(ManualUpdateMethod)EditorGUILayout.EnumPopup("Update Method :",
                                (ManualUpdateMethod)manualUpdateMethodProp.enumValueIndex);

                        if (manualUpdateMethodProp.enumValueIndex == (int)ManualUpdateMethod.Periodical)
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateFrequencyInSeconds"),
                                new GUIContent("Update Frequency (s)"));
                        break;
                    case BindingMethod.Static:
                        EditorGUILayout.LabelField("<color=cyan>Bound Statically.</color>", boldLabel);
                        EditorGUILayout.LabelField(
                            "Note : Static class must implement event : PropertyChanged(string propertyName).");
                        break;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Type : <b>{type?.FullName}</b>", label);
            }

            if (GUILayout.Button("Bound Object Type"))
            {
                EditorGUILayout.Space();
                BindingTypeSearchWindow.Popup(type?.FullName, SetBindingType);
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CheckAutoAssign(BindingNode obj, string colorHexa)
        {
            var isMono = typeof(MonoBehaviour).IsAssignableFrom(obj.BindingType) &&
                         obj.GetComponent(obj.BindingType);

            serializedObject.FindProperty("canBeAutoAssigned").boolValue = isMono;

            if (isMono)
            {
                var boldLabelWithRichText = EditorStyles.boldLabel;
                boldLabelWithRichText.richText = true;

                EditorGUILayout.LabelField(
                    $"Component found, can be assigned <color={colorHexa}>automatically</color>.",
                    boldLabelWithRichText);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAssign"));
            }
        }

        private void SetBindingType(string value)
        {
            serializedObject.FindProperty("bindingTypeName").stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}