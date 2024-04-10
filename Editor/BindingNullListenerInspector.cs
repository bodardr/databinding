using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(NullBindingListener), true)]
    public class BindingNullListenerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var boldStyle = EditorStyles.boldLabel;
            boldStyle.richText = true;

            var bindingListener = (NullBindingListener)target;
            bool synchroWithListener = bindingListener.gameObject.GetComponent<BindingListener>() != null;

            DrawBindingListenerGUI();

            EditorGUILayout.Space();

            if (synchroWithListener)
            {
                EditorGUILayout.LabelField(
                    "<color=cyan>Linked with Binding Listener. Will change value only if the object is set null.</color>",
                    boldStyle);
            }

            var nullProp = serializedObject.FindProperty("nullValue");
            var notNullProp = serializedObject.FindProperty("notNullValue");

            Type setterType = null;

            if (bindingListener.SetExpression.AssemblyQualifiedTypeNames.Length > 0 &&
                !string.IsNullOrEmpty(bindingListener.SetExpression.AssemblyQualifiedTypeNames[1]))
                setterType = Type.GetType(bindingListener.SetExpression.AssemblyQualifiedTypeNames[1]);

            if (setterType != null)
            {
                if (!synchroWithListener)
                    nullProp.DrawGenericSerializedObject(setterType);

                notNullProp.DrawGenericSerializedObject(setterType);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("nullEvent"));

            if (!synchroWithListener)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("notNullEvent"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("changesSetActive"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("invert"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBindingListenerGUI()
        {
            var bindingNode = BindingInspectorCommon.DrawSearchStrategy(serializedObject);

            if (bindingNode == null)
                return;

            var setExprProp = serializedObject.FindProperty("setExpression");
            EditorGUILayout.PropertyField(setExprProp);
        }
    }
}