using System;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor.Expressions
{
    [CustomPropertyDrawer(typeof(BindingSetExpression), true)]
    public class SetExpressionDrawer : PropertyDrawer
    {
        private const float buttonWidth = 25;

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var serializedObject = property.serializedObject;
            var bindingBehaviorProp = serializedObject.FindProperty("bindingBehavior");

            if (bindingBehaviorProp is not { propertyType: SerializedPropertyType.ObjectReference })
            {
                EditorGUI.LabelField(position, "bindingBehavior not found or isn't of type : BindingBehavior");
                return;
            }

            if (bindingBehaviorProp.objectReferenceValue == null)
                return;

            var labelRect = new Rect(position)
            {
                width = position.width - buttonWidth
            };

            position.x += labelRect.width;
            position.width -= labelRect.width;
            var buttonRect = new Rect(position);

            var setPath = property.FindPropertyRelative("path");

            EditorGUI.LabelField(labelRect,
                $"<b>Target Property :</b> {(string.IsNullOrEmpty(setPath.stringValue) ? "<i>Please Specify</i>" : setPath.stringValue)}",
                BindingInspectorCommon.RichTextStyle);

            if (GUI.Button(buttonRect, "..."))
            {
                PropertyTargetSearchWindow.Popup(serializedObject, property.FindPropertyRelative("path").stringValue,
                    ((MonoBehaviour)serializedObject.targetObject).gameObject, SetTargetPath, SetComponentType);
            }
        }


        private static void SetTargetPath(SerializedObject serializedObject, string value, Type setterType)
        {
            if (serializedObject.targetObject)
                Undo.RecordObject(serializedObject.targetObject, "Set Binding Target Path");

            var setPath = serializedObject.FindProperty("setExpression").FindPropertyRelative("path");
            setPath.stringValue = value;

            var array = serializedObject.FindProperty("setExpression")
                .FindPropertyRelative("assemblyQualifiedTypeNames");
            array.arraySize = 2;
            var typeProp = serializedObject.FindProperty("setExpression")
                .FindPropertyRelative("assemblyQualifiedTypeNames")
                .GetArrayElementAtIndex(1);
            typeProp.stringValue = setterType.AssemblyQualifiedName;

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetComponentType(SerializedObject serializedObject, Type value)
        {
            var array = serializedObject.FindProperty("setExpression")
                .FindPropertyRelative("assemblyQualifiedTypeNames");
            array.arraySize = 2;

            var componentType = array
                .GetArrayElementAtIndex(0);
            componentType.stringValue = value.AssemblyQualifiedName;
            serializedObject.ApplyModifiedProperties();
        }
    }
}