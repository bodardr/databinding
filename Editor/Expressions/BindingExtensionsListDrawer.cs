using System;
using System.Linq;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Bodardr.Databinding.Editor
{
// Static utility class for drawing IBindingExtension lists
    public static class BindingExtensionListDrawer
    {
        public static void CreateBindingExtensionListGUI(SerializedProperty listProperty,
            out VisualElement button, out VisualElement list)
        {
            button = new Button(() => ShowAddExtensionMenu(listProperty))
            {
                text = "+"
            };

            var listView = new ListView
            {
                name = "extension-list-view",
                reorderable = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                selectionType = SelectionType.None, 
                allowRemove = true
            };

            listView.itemsSource = Enumerable.Range(0, listProperty.arraySize).ToList();
            listView.makeItem = () =>
            {
                var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                return root;
            };

            listView.bindItem = (element, index) =>
            {
                element.Clear();
                if (index < listProperty.arraySize)
                {
                    var itemElement = CreateElementContainer(listProperty, index);
                    element.Add(itemElement);
                }
            };

            listView.itemsAdded += _ => UpdateListDisplay(listView, listProperty);
            listView.itemsRemoved
                += _ => UpdateListDisplay(listView, listProperty);
            
            list = listView;
            // Bind to SerializedProperty so it auto-refreshes
            listView.BindProperty(listProperty);
        }
        private static void UpdateListDisplay(ListView listView, SerializedProperty listProperty)
        {
            listView.style.visibility = 
                listProperty == null || listProperty.arraySize <= 0
                    ? Visibility.Hidden: Visibility.Visible;
        }

        private static VisualElement CreateElementContainer(SerializedProperty listProperty, int index)
        {
            var elementProperty = listProperty.GetArrayElementAtIndex(index);

            var elementContainer = new VisualElement();
            elementContainer.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.2f));
            elementContainer.style.borderBottomColor = new StyleColor(Color.gray);
            elementContainer.style.paddingBottom = 4;
            elementContainer.style.paddingTop = 4;

            // Header
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.alignItems = Align.Center;

            string elementLabel = $"{index}: {elementProperty.managedReferenceValue?.GetType().Name ?? "None"}";

            var label = new Label(elementLabel) { style = { unityFontStyleAndWeight = FontStyle.Bold } };

            var buttonContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var removeButton = new Button(() =>
            {
                listProperty.DeleteArrayElementAtIndex(index);
                listProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "×",
                style = { width = 20, height = 20 }
            };

            buttonContainer.Add(removeButton);

            headerContainer.Add(label);
            headerContainer.Add(buttonContainer);
            elementContainer.Add(headerContainer);

            // Property field for the element
            if (elementProperty.managedReferenceValue != null)
            {
                var propertyField = new PropertyField(elementProperty) { label = "" };
                propertyField.Bind(listProperty.serializedObject);
                elementContainer.Add(propertyField);
            }
            else
            {
                var nullLabel = new Label("(None) - Select a type")
                {
                    style =
                    {
                        color = new StyleColor(Color.gray),
                        unityFontStyleAndWeight = FontStyle.Italic
                    }
                };
                elementContainer.Add(nullLabel);
            }

            return elementContainer;
        }

        private static void ShowAddExtensionMenu(SerializedProperty listProperty)
        {
            var menu = new GenericMenu();
            TypeCache.TypeCollection bindingExtensionTypes = TypeCache.GetTypesDerivedFrom<IBindingExtension>();

            foreach (var type in bindingExtensionTypes)
            {
                string menuPath = type.Name;
                if (!string.IsNullOrEmpty(type.Namespace))
                    menuPath = type.Namespace.Replace('.', '/') + "/" + type.Name;

                menu.AddItem(new GUIContent(menuPath), false, () =>
                {
                    var newIndex = listProperty.arraySize;
                    listProperty.InsertArrayElementAtIndex(newIndex);
                    var newElement = listProperty.GetArrayElementAtIndex(newIndex);
                    newElement.managedReferenceValue = Activator.CreateInstance(type);
                    listProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            if (bindingExtensionTypes.Count == 0)
                menu.AddDisabledItem(new GUIContent("No IBindingExtension implementations found"));

            menu.ShowAsContext();
        }
    }
}
