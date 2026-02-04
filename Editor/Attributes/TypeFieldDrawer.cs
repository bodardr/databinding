using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Bodardr.Databinding.Editor
{
    [CustomPropertyDrawer(typeof(TypeFieldAttribute), true)]
    public class TypeFieldDrawer : PropertyDrawer
    {
        private const float buttonWidth = 25;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // 1. Create the container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.height = EditorGUIUtility.singleLineHeight;

            // 2. The Label (supports Rich Text tags like <b> and <color>)
            var typeLabel = new Label();
            typeLabel.style.flexGrow = 1;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            // 3. The Edit Button
            var editButton = new Button(() => 
            {
                BindingSearchWindow.Open(new BindingSearchCriteria(true), (_, type) => SetBindingType(property, type));
            });
            
            editButton.style.width = 25;
            var icon = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D;
            editButton.style.backgroundImage = icon;
            editButton.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;

            // 5. Setup Refresh Logic
            RefreshLabel();

            // This replaces the need for UpdateShow/OnGUI loops
            container.TrackPropertyValue(property, _ => RefreshLabel());
            container.Add(typeLabel);
            container.Add(editButton);

            return container;

            void RefreshLabel()
            {
                var assemblyQualifiedName = property.stringValue;
                var typeName = string.IsNullOrEmpty(assemblyQualifiedName) 
                    ? "None" 
                    : Type.GetType(assemblyQualifiedName)?.Name ?? "Invalid Type";

                typeLabel.text = $"{property.displayName}: <color=yellow><b>{typeName}</b></color>";
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelRect = new Rect(position);
            labelRect.width = position.width - buttonWidth;

            position.x += labelRect.width;
            position.width -= labelRect.width;
            var buttonRect = new Rect(position);
            
            var assemblyQualifiedName = property.stringValue;
            EditorGUI.LabelField(labelRect,
                $"{property.displayName}: <color=yellow><b>{(string.IsNullOrEmpty(assemblyQualifiedName) ? "" : Type.GetType(assemblyQualifiedName)?.FullName)}</b></color>",
                BindingInspectorCommon.RichTextStyle);

            if (GUI.Button(buttonRect, EditorGUIUtility.IconContent("editicon.sml")))
                BindingSearchWindow.Open(new BindingSearchCriteria(true), (_, type) => SetBindingType(property, type));
        }

        private void SetBindingType(SerializedProperty property, List<BindingPropertyEntry> type)
        {
            property.stringValue = type[0].AssemblyQualifiedTypeName;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
