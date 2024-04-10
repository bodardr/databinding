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

        public static void DrawTargetPathLabel(SerializedObject serializedObject)
        {
            var setPath = serializedObject.FindProperty("setExpression").FindPropertyRelative("path");

            EditorGUILayout.LabelField(
                $"<b>Target Property :</b> {(string.IsNullOrEmpty(setPath.stringValue) ? "<i>Please Specify</i>" : setPath.stringValue)}",
                RichTextStyle);
        }
    }
}