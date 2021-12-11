using System;
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

        public static BindingBehavior DrawSearchStrategy(SerializedObject serializedObject)
        {
            Init();

            var bindingBehaviorProperty = serializedObject.FindProperty("bindingBehavior");

            var bindingBehavior = bindingBehaviorProperty.objectReferenceValue as BindingBehavior;
            if (bindingBehavior == null)
                EditorGUILayout.HelpBox(
                    "Binding Behavior cannot be found. Change search strategy or specify a reference manually (Specify Reference)",
                    MessageType.Error);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchStrategy"));
            EditorGUILayout.PropertyField(bindingBehaviorProperty);

            serializedObject.ApplyModifiedProperties();
            return bindingBehavior;
        }

        public static void DrawTargetPathLabel(SerializedObject serializedObject)
        {
            var setPath = serializedObject.FindProperty("setExpression").FindPropertyRelative("path");

            EditorGUILayout.LabelField(
                $"<b>Target Property :</b> {(string.IsNullOrEmpty(setPath.stringValue) ? "<i>Please Specify</i>" : setPath.stringValue)}",
                RichTextStyle);
        }
    }
}