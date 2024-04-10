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
            var bindingNodeProp = property.serializedObject.FindProperty("bindingNode");

            if (bindingNodeProp is not { propertyType: SerializedPropertyType.ObjectReference })
            {
                EditorGUI.LabelField(position, "bindingNode not found or isn't of type : BindingNode");
                return;
            }

            if (bindingNodeProp.objectReferenceValue == null)
            {
                EditorGUI.LabelField(position, "<color=red>bindingNode isn't assigned</color>",
                    BindingInspectorCommon.RichTextStyle);
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

            EditorGUI.LabelField(labelRect,
                $"<b>Bound Property :</b> {(string.IsNullOrEmpty(getPath.stringValue) ? "<i>Please Specify</i>" : getPath.stringValue)}",
                BindingInspectorCommon.RichTextStyle);

            if (GUI.Button(buttonRect, "..."))
            {
                var bindingNode = (BindingNode)bindingNodeProp.objectReferenceValue;
                PropertySearchWindow.Popup(property, bindingNode.BindingType, SetPropertyPath);
            }
        }

        private static void SetPropertyPath(SerializedProperty serializedProperty, string value, Type[] types)
        {
            Undo.RecordObject(serializedProperty.serializedObject.targetObject, "Set Binding Property Path");

            var getPath = serializedProperty.FindPropertyRelative("path");
            getPath.stringValue = value;

            var array = serializedProperty.FindPropertyRelative("assemblyQualifiedTypeNames");
            array.arraySize = types.Length;

            var i = 0;
            foreach (SerializedProperty element in array)
            {
                element.stringValue = types[i].AssemblyQualifiedName;
                i++;
            }

            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}