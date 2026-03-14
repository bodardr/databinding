using System;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bodardr.Databinding.Editor
{
    [CustomPropertyDrawer(typeof(BindingGetExpression), true)]
    public class BindingGetExpressionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create the root container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.alignItems = Align.Center;

            // Create the label container (takes remaining space)
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1;
            labelContainer.style.flexShrink = 1;

            // Create the label using the common method
            // Note: You'll need to adapt BindingInspectorCommon.DrawLabel for UI Elements
            // For now, creating a simple label as placeholder
            var label = new Label("Source");
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            labelContainer.Add(label);

            // Create the button
            var button = new Button(() => OnButtonClick(property));

            // Set the button icon (edit icon)
            var icon = EditorGUIUtility.IconContent("editicon.sml");
            button.iconImage = (Texture2D)icon.image;

            var extensions = property.FindPropertyRelative("extensions");
            BindingExtensionListDrawer.CreateBindingExtensionListGUI(
                extensions, out var addExtensionButton, out var list);

            // Add elements to root
            mainRow.Add(labelContainer);

            mainRow.Add(addExtensionButton);
            mainRow.Add(button);

            container.Add(mainRow);
            container.Add(list);

            return container;
        }

        private void OnButtonClick(SerializedProperty property)
        {
            BindingExpressionPathValidator.TryFixingPath(property, (BindingGetExpression)property.boxedValue);
                var searchCriteria = new BindingSearchCriteria(property);

            var enumValues = Enum.GetValues(typeof(BindingExpressionLocation));
            if (property.FindPropertyRelative("location").enumValueIndex ==
                Array.IndexOf(enumValues, BindingExpressionLocation.None))
                searchCriteria.Location = searchCriteria.BindingNode == null ? BindingExpressionLocation.Static
                    : BindingExpressionLocation.InBindingNode;

            searchCriteria.Flags = BindingSearchCriteria.PropertyFlag.Getter;
            
            BindingSearchWindow.Open(searchCriteria,
                (location, entries) => EditorDatabindingUtility.SetTargetPath(property, location, entries));
        }
    }
}
