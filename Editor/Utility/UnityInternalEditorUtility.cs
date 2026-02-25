using System;
using System.Collections.Generic;
using System.Reflection;
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

        public static void DrawViaProxySerializedObject(string label, object value, Type type,
            Action<object> onValueChanged)
        {
            if (proxy == null) proxy = ScriptableObject.CreateInstance<ProxyHost>();

            var valid = value != null && value.GetType().IsAssignableFrom(type);
            var isString = type == typeof(string);
            proxy.payload = valid ? value : isString ? string.Empty : Activator.CreateInstance(type);

            if (serializedProxy == null || serializedProxy.targetObject != proxy)
                serializedProxy = new SerializedObject(proxy);

            serializedProxy.Update();

            // Find the payload property. Because of SerializeReference, 
            // Unity will generate a full tree for the struct's fields.
            var payloadProp = serializedProxy.FindProperty(nameof(ProxyHost.payload));

            EditorGUI.BeginChangeCheck();
            if (isString)
                proxy.payload = EditorGUILayout.TextField(label, (string)proxy.payload);
            else
                EditorGUILayout.PropertyField(payloadProp, new GUIContent(label), true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedProxy.ApplyModifiedProperties();
                onValueChanged?.Invoke(proxy.payload);
            }
        }
    }
}
