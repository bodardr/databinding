#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bodardr.Databinding.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Bodardr.Databinding.Editor
{
    public static class BindingListenerExtensions
    {
        [MenuItem("CONTEXT/BindingListener/Databinding - To Formatted Listener", true, 1)]
        public static bool ValidateConvertToBindingListener(MenuCommand menuCommand)
        {
            return menuCommand.context.GetType() == typeof(BindingListener);
        }

        [MenuItem("CONTEXT/BindingListener/Databinding - To Formatted Listener", false, 1)]
        public static void ConvertToFormattedListener(MenuCommand menuCommand)
        {
            var bindingListener = (BindingListener)menuCommand.context;
            var formattedBindingListener = bindingListener.gameObject.AddComponent<FormattedBindingListener>();
            
            formattedBindingListener.GetExpression = bindingListener.GetExpression;
            formattedBindingListener.SetExpression = bindingListener.SetExpression;

            var formattedCtx = new SerializedObject(formattedBindingListener);
            formattedCtx.FindProperty("format").stringValue = "{0}";
            formattedCtx.ApplyModifiedPropertiesWithoutUndo();

            Undo.DestroyObjectImmediate(bindingListener);
        }

        [MenuItem("CONTEXT/FormattedBindingListener/Databinding - To Binding Listener", false, 2)]
        public static void ConvertToBindingListener(MenuCommand menuCommand)
        {
            var formattedBindingListener = (FormattedBindingListener)menuCommand.context;
            var bindingListener = formattedBindingListener.gameObject.AddComponent<BindingListener>();

            bindingListener.GetExpression = formattedBindingListener.GetExpression;
            bindingListener.SetExpression = formattedBindingListener.SetExpression;

            var bindingListenerObj = new SerializedObject(bindingListener);
            bindingListenerObj.ApplyModifiedProperties();
            Undo.DestroyObjectImmediate(formattedBindingListener);
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Formatted Listener")]
        public static void AddFormattedTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<FormattedBindingListener>();
            EditorDatabindingUtility.SetExpressionPathManually(bindingListener, menuCommand.context, "setExpression",
                new List<BindingPropertyEntry>
                {
                    new(typeof(TextMeshProUGUI)),
                    new(typeof(string), nameof(TextMeshProUGUI.text))
                }, BindingExpressionLocation.InGameObject);
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Listener")]
        public static void AddTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            EditorDatabindingUtility.SetExpressionPathManually(bindingListener, menuCommand.context, "setExpression",
                new List<BindingPropertyEntry>
                {
                    new(typeof(TextMeshProUGUI)),
                    new(typeof(string), nameof(TextMeshProUGUI.text))
                }, BindingExpressionLocation.InGameObject);
        }

        [MenuItem("CONTEXT/Image/Databinding - Add Listener")]
        public static void AddImageBindingListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            EditorDatabindingUtility.SetExpressionPathManually(bindingListener, menuCommand.context, "setExpression",
                new List<BindingPropertyEntry>
                {
                    new(typeof(Image)),
                    new(typeof(Sprite), nameof(Image.sprite))
                }, BindingExpressionLocation.InGameObject);
        }

        [MenuItem("CONTEXT/Button/Databinding - Add Listener")]
        public static void AddBindingButtonListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            EditorDatabindingUtility.SetExpressionPathManually(bindingListener, menuCommand.context, "setExpression",
                new List<BindingPropertyEntry>
                {
                    new(typeof(Button)),
                    new(typeof(bool), nameof(Button.interactable))
                }, BindingExpressionLocation.InGameObject);
        }

        [MenuItem("CONTEXT/BindingCollectionBehavior/Databinding - Add Listener")]
        public static void AddBindingCollectionListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.SetExpression.Path = "BindingCollectionBehavior.Collection";

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(BindingCollectionBehavior).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(IEnumerable).AssemblyQualifiedName;
        }

        [MenuItem("CONTEXT/Image/Databinding - Add Enum Listener")]
        public static void AddEnumListenerFromImage(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<EnumBindingListener>();

            bindingListener.SetExpression.Path = "Image.sprite";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] = typeof(Image).AssemblyQualifiedName;
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] = typeof(Sprite).AssemblyQualifiedName;
        }

        [MenuItem("CONTEXT/Image/Databinding - Add Conditional Listener")]
        public static void AddImageListener(MenuCommand menuCommand)
        {
            var bindingListener =
                ((Component)menuCommand.context).gameObject.AddComponent<ConditionalBindingListener>();

            bindingListener.SetExpression.Path = "Image.sprite";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] = typeof(Image).AssemblyQualifiedName;
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] = typeof(Sprite).AssemblyQualifiedName;
        }
    }
}
#endif
