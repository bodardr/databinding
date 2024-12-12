using System;
using System.Collections.Generic;
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
            var bindingTypeName = serializedObject.FindProperty("bindingTypeName").stringValue;
            var type = Type.GetType(bindingTypeName);

            if (string.IsNullOrEmpty(bindingTypeName))
            {
                EditorGUILayout.LabelField("No type selected. Define a type below.", SearchWindowsCommon.errorStyle);
            }
            else if (type == null)
            {
                EditorGUILayout.LabelField(
                    $"Type with full name \n<b>{bindingTypeName}</b>\n doesn't exist or is invalid. Change the type below.",
                    SearchWindowsCommon.errorStyle);
            }
            else
            {
                var bindingMethodProp = serializedObject.FindProperty("bindingMethod");
                var bindingMethod = (BindingMethod)bindingMethodProp.enumValueIndex;

                string bindingMethodStr = null;
                string bindingMethodNote = null;

                switch (bindingMethod)
                {
                    case BindingMethod.Dynamic:
                        bindingMethodStr = "<color=#22e05b>Bound Dynamically</color>";
                        break;
                    case BindingMethod.Manual:
                        bindingMethodStr = "<color=#514fc4>Bound Manually.</color>";
                        bindingMethodNote = "<b>Note :</b> Must be updated manually.";
                        break;
                    case BindingMethod.Static:
                        bindingMethodStr = "<color=cyan>Bound Statically.</color>";
                        bindingMethodNote =
                            "<b>Note :</b> Static class must implement event : <b>PropertyChanged</b>(string propertyName)";
                        break;
                }

                EditorGUILayout.LabelField($"<b>Type : <color=yellow>{type.Name}</color></b>", label);
                EditorGUILayout.LabelField(bindingMethodStr, boldLabel);

                if (!string.IsNullOrEmpty(bindingMethodNote))
                    EditorGUILayout.LabelField(bindingMethodNote, label);

                if (bindingMethod != BindingMethod.Static)
                    CheckAutoAssign(obj, "#22e05b");

                EditorGUILayout.PropertyField(serializedObject.FindProperty("performTypeChecks"));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Bound Object Type"))
                BindingSearchWindow.Open(new BindingSearchCriteria(true), SetBindingType);

            serializedObject.ApplyModifiedProperties();
        }

        private void CheckAutoAssign(BindingNode obj, string colorHex)
        {
            var isComponent = typeof(Component).IsAssignableFrom(obj.BindingType) &&
                obj.GetComponent(obj.BindingType);

            serializedObject.FindProperty("canBeAutoAssigned").boolValue = isComponent;

            if (isComponent)
            {
                EditorGUILayout.Space();
                var boldLabelWithRichText = new GUIStyle(EditorStyles.boldLabel);
                boldLabelWithRichText.richText = true;

                EditorGUILayout.LabelField(
                    $"Component found in GameObject, can be assigned <color={colorHex}>On Start</color>.",
                    boldLabelWithRichText);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAssign"));
            }
        }

        private void SetBindingType(BindingExpressionLocation bindingExpressionLocation, List<BindingPropertyEntry> bindingPropertyEntries)
        {
            serializedObject.FindProperty("bindingTypeName").stringValue = bindingPropertyEntries[0].AssemblyQualifiedTypeName;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
