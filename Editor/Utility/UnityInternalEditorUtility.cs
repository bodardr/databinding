using System;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public static class UnityInternalEditorUtility
    {
        private class ProxyHost : ScriptableObject
        {
            [SerializeReference] public object payload;
        }

        private static ProxyHost proxy;
        private static SerializedObject serializedProxy;

        public static void DrawStructFieldViaProxy(string label, object value, Type type,
            Action<object> onValueChanged)
        {
            if (proxy == null) proxy = ScriptableObject.CreateInstance<ProxyHost>();
            proxy.payload = value;

            if (serializedProxy == null || serializedProxy.targetObject != proxy)
                serializedProxy = new SerializedObject(proxy);
            serializedProxy.Update();

            var payloadProp = serializedProxy.FindProperty(nameof(ProxyHost.payload));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(payloadProp, new GUIContent(label), true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedProxy.ApplyModifiedProperties();
                onValueChanged?.Invoke(proxy.payload);
            }
        }
    }
}
