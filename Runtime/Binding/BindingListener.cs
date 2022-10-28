using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
#endif

namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Listener")]
    public class BindingListener : BindingListenerBase
    {
        [SerializeField]
        protected BindingGetExpression getExpression = new();

        [SerializeField]
        protected BindingSetExpression setExpression = new();

        protected Component component;

        public BindingGetExpression GetExpression => getExpression;

        public BindingSetExpression SetExpression => setExpression;

        protected override void Awake()
        {
            base.Awake();

            component = GetComponent(Type.GetType(SetExpression.AssemblyQualifiedTypeNames[0]));

            var go = gameObject;
            GetExpression.ResolveExpression(go);
            SetExpression.ResolveExpression(go);

            bindingBehavior.AddListener(this, GetExpression.Path);
        }

        public override void QueryExpressions(Dictionary<string, Tuple<IBindingExpression, GameObject>> expressions)
        {
            var go = gameObject;

            if (!GetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(GetExpression.Path))
                expressions.Add(GetExpression.Path, new(GetExpression, go));

            if (!SetExpression.ExpressionAlreadyCompiled && !expressions.ContainsKey(SetExpression.Path))
                expressions.Add(SetExpression.Path, new(SetExpression, go));
        }

        public override void OnBindingUpdated(object obj)
        {
            CheckForInitialization();

            try
            {
                var fetchedValue = GetExpression.Expression(obj);
                SetExpression.Expression(component, fetchedValue);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"<b><color=red>Error with expressions {GetExpression.Path} / {SetExpression.Path} in {gameObject.name}</color></b> {e}",
                    this);
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

        [MenuItem("CONTEXT/Button/Databinding - Add Listener")]
        public static void AddButtonListener(MenuCommand menuCommand)
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
#endif
    }
}