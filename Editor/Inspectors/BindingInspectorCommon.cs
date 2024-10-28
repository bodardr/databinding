using System;
using System.Text;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public static class BindingInspectorCommon
    {
        public static GUIStyle RichTextStyle;
        private static bool init = false;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (init)
                return;

            RichTextStyle = new GUIStyle { richText = true, normal = new GUIStyleState { textColor = Color.white } };

            init = true;
        }

        public static BindingNode DrawSearchStrategy(SerializedObject serializedObject)
        {
            Init();

            var bindingNodeProperty = serializedObject.FindProperty("bindingNode");

            var bindingNode = bindingNodeProperty.objectReferenceValue as BindingNode;
            if (bindingNode == null)
                EditorGUILayout.HelpBox(
                    "Binding Behavior cannot be found. Change search strategy or specify a reference manually (Specify Reference)",
                    MessageType.Error);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchStrategy"));
            EditorGUILayout.PropertyField(bindingNodeProperty);

            serializedObject.ApplyModifiedProperties();
            return bindingNode;
        }
        
        public static void DrawLabel(string labelPrefix, SerializedProperty property, Rect rect)
        {
            var setPath  = property.FindPropertyRelative("path");
            
            var assemblyQualifiedNames = property.FindPropertyRelative("assemblyQualifiedTypeNames");
            var lastAssemblyQualifiedName = assemblyQualifiedNames.arraySize > 0 ? assemblyQualifiedNames
                .GetArrayElementAtIndex(assemblyQualifiedNames.arraySize - 1).stringValue : string.Empty;

            var str = new StringBuilder();
            str.Append($"<b>{labelPrefix} :</b> \t");

            if (!string.IsNullOrEmpty(lastAssemblyQualifiedName))
                str.Append($"(<color=yellow><b>{Type.GetType(lastAssemblyQualifiedName)?.Name}</b></color>) ");

            str.Append(string.IsNullOrEmpty(setPath.stringValue) ? "<i>Undefined</i>" : setPath.stringValue);

            EditorGUI.LabelField(rect, str.ToString(), RichTextStyle);
        }
    }
}
