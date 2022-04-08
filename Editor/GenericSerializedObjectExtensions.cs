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
        private static bool initialized;

        private static Dictionary<Type, Func<string, object, object>> propertyFields;

        static GenericSerializedObjectExtensions()
        {
            propertyFields = new Dictionary<Type, Func<string, object, object>>
            {
                { typeof(Vector2Int), (str, obj) => EditorGUILayout.Vector2IntField(str, (Vector2Int)obj) },
                { typeof(Vector3Int), (str, obj) => EditorGUILayout.Vector3IntField(str, (Vector3Int)obj) },
                { typeof(Vector2), (str, obj) => EditorGUILayout.Vector2Field(str, (Vector2)obj) },
                { typeof(Vector3), (str, obj) => EditorGUILayout.Vector3Field(str, (Vector3)obj) },
                { typeof(Vector4), (str, obj) => EditorGUILayout.Vector4Field(str, (Vector4)obj) },
                { typeof(bool), (str, obj) => EditorGUILayout.Toggle(str, (bool)obj) },
                { typeof(int), (str, obj) => EditorGUILayout.IntField(str, (int)obj) },
                { typeof(float), (str, obj) => EditorGUILayout.FloatField(str, (float)obj) },
                { typeof(double), (str, obj) => EditorGUILayout.DoubleField(str, (double)obj) },
                { typeof(long), (str, obj) => EditorGUILayout.LongField(str, (long)obj) },
                { typeof(Color), (str, obj) => EditorGUILayout.ColorField(str, (Color)obj) },
                { typeof(AnimationCurve), (str, obj) => EditorGUILayout.CurveField(str, (AnimationCurve)obj) },
                { typeof(Gradient), (str, obj) => EditorGUILayout.GradientField(str, (Gradient)obj) },
                { typeof(Bounds), (str, obj) => EditorGUILayout.BoundsField(str, (Bounds)obj) },
                { typeof(BoundsInt), (str, obj) => EditorGUILayout.BoundsIntField(str, (BoundsInt)obj) },
                { typeof(Rect), (str, obj) => EditorGUILayout.RectField(str, (Rect)obj) },
                { typeof(string), (str, obj) => EditorGUILayout.TextField(str, (string)obj) }
            };
        }

        public static void DrawGenericSerializedObject(this SerializedProperty prop, Type setterMemberType,
            string propDisplayName = "")
        {
            if (string.IsNullOrEmpty(propDisplayName))
                propDisplayName = prop.displayName;

            GenericSerializedObject obj = (GenericSerializedObject)prop.GetValue();

            var objectValue = obj.Value;

            var valid = objectValue != null && objectValue.GetType().IsAssignableFrom(setterMemberType);

            if (propertyFields.ContainsKey(setterMemberType))
            {
                if (!valid)
                    objectValue = setterMemberType != typeof(string) ? Activator.CreateInstance(setterMemberType) : "";

                var func = propertyFields[setterMemberType];
                obj.Value = func.Invoke(propDisplayName, objectValue);
            }
            else if (setterMemberType.IsEnum)
            {
                var values = Enum.GetNames(setterMemberType);

                if (!valid)
                    objectValue = 0;

                obj.Value = EditorGUILayout.Popup((int)objectValue, values);
            }
            else
            {
                if (!valid)
                    objectValue = null;

                obj.Value = EditorGUILayout.ObjectField(propDisplayName, (Object)objectValue, setterMemberType, true);
            }

            if (objectValue == null && obj.Value != null || 
                objectValue != null && !objectValue.Equals(obj.Value))
            {
                prop.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(prop.serializedObject.targetObject);
            }
        }
    }
}