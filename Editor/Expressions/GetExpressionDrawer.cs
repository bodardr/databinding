using System;
using Bodardr.Databinding.Runtime;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor.Expressions
{
    [CustomPropertyDrawer(typeof(BindingGetExpression), true)]
    public class GetExpressionDrawer : PropertyDrawer
    {
        private const float buttonWidth = 25;
        
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var bindingBehaviorProp = property.serializedObject.FindProperty("bindingBehavior");

            if (bindingBehaviorProp is not { propertyType: SerializedPropertyType.ObjectReference })
            {
                EditorGUI.LabelField(position, "bindingBehavior not found or isn't of type : BindingBehavior");
                return;
            }

            if (bindingBehaviorProp.objectReferenceValue == null)
            {
                EditorGUI.LabelField(position, "<color=red>bindingBehavior isn't assigned</color>", BindingInspectorCommon.RichTextStyle);
                return;
            }

            var labelRect = new Rect(position)
            {
                width = position.width - buttonWidth
            };

            position.x += labelRect.width;
            position.width -= labelRect.width;
            var buttonRect = new Rect(position);

            var getPath = property.FindPropertyRelative("path");

            EditorGUI.LabelField(labelRect, $"<b>Bound Property :</b> {(string.IsNullOrEmpty(getPath.stringValue) ? "<i>Please Specify</i>" : getPath.stringValue)}",
                BindingInspectorCommon.RichTextStyle);

            if (GUI.Button(buttonRect, "..."))
            {
                var bindingBehavior = (BindingBehavior)bindingBehaviorProp.objectReferenceValue;
                PropertySearchWindow.Popup(property, bindingBehavior.BoundObjectType, SetPropertyPath);
            }
        }

        private static void SetPropertyPath(SerializedProperty serializedProperty, string value, Type[] types)
        {
            var getPath = serializedProperty.FindPropertyRelative("path");
            getPath.stringValue = value;


            var array = serializedProperty.FindPropertyRelative("assemblyQualifiedTypeNames");
            array.arraySize = 2;

            var boundObjectTypeProp = array.GetArrayElementAtIndex(0);
            boundObjectTypeProp.stringValue = types[0].AssemblyQualifiedName;

            var memberTypeProp = array.GetArrayElementAtIndex(1);
            memberTypeProp.stringValue = types[1].AssemblyQualifiedName;

            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}