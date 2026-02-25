using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bodardr.Databinding.Editor
{
    public static class GenericSerializedObjectExtensions
    {
        public static void DrawGenericSerializedObject(this SerializedProperty prop, Type setterMemberType,
            string propDisplayName = "")
        {
            if (string.IsNullOrEmpty(propDisplayName)) propDisplayName = prop.displayName;
            var genericSerializedObject = (GenericSerializedObject)prop.boxedValue;
            var objectValue = genericSerializedObject.Value;

            if (setterMemberType.IsValueType || setterMemberType == typeof(string))
            {
                UnityInternalEditorUtility.DrawViaProxySerializedObject(propDisplayName, objectValue, setterMemberType,
                    newValue =>
                    {
                        genericSerializedObject.Value = newValue;
                        prop.boxedValue = genericSerializedObject;
                        prop.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prop.serializedObject.targetObject);
                    });
            }
            else
            {
                // Fallback for UnityEngine.Object (References)
                EditorGUI.BeginChangeCheck();
                if (objectValue is not Object)
                    objectValue = null;
                
                var result = EditorGUILayout.ObjectField(propDisplayName, (Object)objectValue, setterMemberType, true);
                if (EditorGUI.EndChangeCheck())
                {
                    genericSerializedObject.Value = result;
                    prop.boxedValue = genericSerializedObject;
                    prop.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(prop.serializedObject.targetObject);
                }
            }
        }
    }
}
