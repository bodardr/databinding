using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(BindingListener))]
    [CanEditMultipleObjects]
    public class BindingListenerInspector : UnityEditor.Editor
    {
        private static GUIStyle richTextStyle;
        private static bool init = false;

        private BindingListener bindingListener;

        private static void Init()
        {
            if (init)
                return;

            richTextStyle = new GUIStyle { richText = true, normal = new GUIStyleState { textColor = Color.white } };

            init = true;
        }

        public override void OnInspectorGUI()
        {
            Init();

            var bindingBehaviorProperty = serializedObject.FindProperty("bindingBehavior");
            bindingListener = (BindingListener)target;

            var bindingBehavior = bindingBehaviorProperty.objectReferenceValue as BindingBehavior;
            if (bindingBehavior == null)
                EditorGUILayout.HelpBox(
                    "Binding Behavior cannot be found. Change search strategy or specify a reference manually (Specify Reference)",
                    MessageType.Error);

            var searchStrategy = serializedObject.FindProperty("searchStrategy");

            EditorGUILayout.PropertyField(searchStrategy);

            //If Search Strategy is set to 'SpecifyReference'
            if (searchStrategy.enumValueIndex == 1)
                EditorGUILayout.PropertyField(bindingBehaviorProperty);

            serializedObject.ApplyModifiedProperties();

            if (bindingBehavior == null)
                return;

            EditorGUILayout.Space();

            var getPath = serializedObject.FindProperty("getPath");
            var setPath = serializedObject.FindProperty("setPath");

            EditorGUILayout.LabelField(
                $"<b>Bound Property :</b> {(string.IsNullOrEmpty(getPath.stringValue) ? "<i>Please Specify</i>" : getPath.stringValue)}",
                richTextStyle);
            if (!string.IsNullOrEmpty(setPath.stringValue))
                EditorGUILayout.LabelField(
                    $"<b>Target Property :</b> {(string.IsNullOrEmpty(setPath.stringValue) ? "<i>Please Specify</i>" : setPath.stringValue)}",
                    richTextStyle);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Bound Property"))
            {
                PropertySearchWindow.Popup(bindingBehavior.BoundObjectType, SetPropertyPath);
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Target"))
            {
                PropertyTargetSearchWindow.Popup(setPath.stringValue,
                    bindingListener.gameObject, SetTargetPath);

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SetPropertyPath(string value)
        {
            var getPath = serializedObject.FindProperty("getPath");

            getPath.stringValue = value;

            serializedObject.ApplyModifiedProperties();
        }


        private void SetTargetPath(string value)
        {
            var setPath = serializedObject.FindProperty("setPath");

            setPath.stringValue = value;

            serializedObject.ApplyModifiedProperties();
        }
    }
}