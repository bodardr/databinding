using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomPropertyDrawer(typeof(BindingSetExpression), true)]
    public class SetExpressionDrawer : PropertyDrawer
    {
        private const float buttonWidth = 25;

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var serializedObject = property.serializedObject;
            var bindingNodeProp = serializedObject.FindProperty("bindingNode");

            if (bindingNodeProp is not { propertyType: SerializedPropertyType.ObjectReference })
            {
                EditorGUI.LabelField(position, "bindingNode not found or isn't of type : BindingNode");
                return;
            }

            if (bindingNodeProp.objectReferenceValue == null)
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


        private static void SetTargetPath(SerializedObject serializedObject, string value, Type[] types)
        {
            if (serializedObject.targetObject)
                Undo.RecordObject(serializedObject.targetObject, "Set Binding Target Path");

            var expression = serializedObject.FindProperty("setExpression");
            var path = expression.FindPropertyRelative("path");
            path.stringValue = value;

            var array = expression.FindPropertyRelative("assemblyQualifiedTypeNames");
            array.arraySize = types.Length;

            var i = 0;
            foreach (SerializedProperty element in array)
            {
                element.stringValue = types[i].AssemblyQualifiedName;
                i++;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetComponentType(SerializedObject serializedObject, Type value)
        {
            var array = serializedObject.FindProperty("setExpression")
                .FindPropertyRelative("assemblyQualifiedTypeNames");

            if (array.arraySize < 1)
                array.arraySize = 1;

            var componentType = array.GetArrayElementAtIndex(0);
            componentType.stringValue = value.AssemblyQualifiedName;
            serializedObject.ApplyModifiedProperties();
        }
    }
}