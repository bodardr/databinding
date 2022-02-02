using System;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine.UI;
#endif

namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Listener")]
    public class BindingListener : BindingListenerBase
    {
        protected Component component;

        [SerializeField]
        protected BindingGetExpression getExpression = new();

        [SerializeField]
        protected BindingSetExpression setExpression = new();

        public BindingGetExpression GetExpression => getExpression;

        public BindingSetExpression SetExpression => setExpression;

        public override void InitializeAndCompile()
        {
            base.InitializeAndCompile();

            GetExpression.Compile();
            SetExpression.Compile();
        }

        protected virtual void Awake()
        {
            if (bindingBehavior == null)
            {
                Debug.LogWarning(
                    "Binding Behavior cannot be found, try changing Search Strategy or specify it manually.");
                return;
            }

            var active = gameObject.activeSelf;
            gameObject.SetActive(true);

            component = GetComponent(Type.GetType(SetExpression.AssemblyQualifiedTypeNames[0]));

            GetExpression.ResolveExpression();
            SetExpression.ResolveExpression();

            gameObject.SetActive(active);
            bindingBehavior.AddListener(this, GetExpression.Path);
        }

        public override void UpdateValue(object obj)
        {
            try
            {
                var fetchedValue = GetExpression.Expression(obj);
                SetExpression.Expression(component, fetchedValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"<b><color=red>Error with expressions {GetExpression.Path} / {SetExpression.Path} in {gameObject.name}</color></b>");
            }
        }

#if UNITY_EDITOR
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
        public static void AddImageListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.SetExpression.Path = "Image.sprite";

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] =
                typeof(Image).AssemblyQualifiedName;

            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] =
                typeof(Sprite).AssemblyQualifiedName;
        }
#endif
    }
}