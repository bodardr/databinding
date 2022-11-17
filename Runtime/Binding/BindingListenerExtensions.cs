#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Bodardr.Databinding.Runtime
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
            var go = ((BindingListener)menuCommand.context).gameObject;
            var ctx = new SerializedObject(menuCommand.context);
            ctx.FindProperty("m_Script").objectReferenceValue = Resources.FindObjectsOfTypeAll<MonoScript>()
                .First(x => x.GetClass() == typeof(FormattedBindingListener));
            ctx.ApplyModifiedPropertiesWithoutUndo();

            var formattedCtx =
                new SerializedObject(go.GetComponent<FormattedBindingListener>());
            formattedCtx.FindProperty("format").stringValue = "{0}";
            formattedCtx.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("CONTEXT/FormattedBindingListener/Databinding - To Binding Listener", false, 2)]
        public static void ConvertToBindingListener(MenuCommand menuCommand)
        {
            var ctx = new SerializedObject(menuCommand.context);
            ctx.FindProperty("m_Script").objectReferenceValue = Resources.FindObjectsOfTypeAll<MonoScript>()
                .First(x => x.GetClass() == typeof(BindingListener));

            ctx.ApplyModifiedPropertiesWithoutUndo();
        }


        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Formatted Listener")]
        public static void AddFormattedTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<FormattedBindingListener>();
            bindingListener.SetExpression.Path = "TextMeshProUGUI.text";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(TextMeshProUGUI).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(string).AssemblyQualifiedName;
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Listener")]
        public static void AddTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.SetExpression.Path = "TextMeshProUGUI.text";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(TextMeshProUGUI).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(string).AssemblyQualifiedName;
        }

        [MenuItem("CONTEXT/Image/Databinding - Add Listener")]
        public static void AddImageBindingListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.SetExpression.Path = "Image.sprite";

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(Image).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(Sprite).AssemblyQualifiedName;
        }

        [MenuItem("CONTEXT/Button/Databinding - Add Listener")]
        public static void AddBindingButtonListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.SetExpression.Path = "Button.interactable";

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(Button).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(bool).AssemblyQualifiedName;
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