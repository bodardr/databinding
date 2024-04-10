 using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Bodardr.Utility.Runtime;
using UnityEngine;

// ReSharper disable PossibleMultipleEnumeration
namespace Bodardr.Databinding.Runtime
{
    public enum ManualUpdateMethod
    {
        Manual,
        Periodical,
        EveryFrame
    }

    public enum BindingMethod
    {
        Dynamic,
        Manual,
        Static
    }

    [AddComponentMenu("Databinding/Binding Node")]
    public class BindingNode : MonoBehaviour
    {
        private readonly List<Tuple<BindingListenerBase, string>> listeners = new();

        /// <summary>
        /// List of nested listeners to be added after the binding update has finished.
        /// </summary>
        private readonly List<Tuple<BindingListenerBase, string>> tempListeners = new();

        private bool isUpdatingBindings = false;

        private object binding;
        private Type bindingType;

        private Delegate updatePropertyCall;
        private EventInfo updatePropertyEvent;

        private static MethodInfo updatePropertyMethod;
        private static bool initializedStatically = false;

        [SerializeField]
        private bool autoAssign = true;

        [SerializeField]
        private bool canBeAutoAssigned = false;

        [SerializeField]
        private BindingMethod bindingMethod = BindingMethod.Static;

        [SerializeField]
        private ManualUpdateMethod manualUpdateMethod;

        [SerializeField]
        private float updateFrequencyInSeconds;

        [SerializeField]
        private string bindingTypeName = "";

        private bool BindingValid =>
            autoAssign || Binding != null || BindingType.IsSealed && BindingType.IsAbstract;

        public Type BindingType => bindingType ??= Type.GetType(bindingTypeName);

        public object Binding
        {
            get
            {
                if (autoAssign && binding == null)
                    HookUsingAutoAssign();

                return binding;
            }
            set
            {
                if (binding != null)
                    UnhookPreviousObject();

#if UNITY_EDITOR
                AssertTypeMatching(value);
#endif

                binding = value;

                switch (bindingMethod)
                {
                    case BindingMethod.Dynamic:
                        ((INotifyPropertyChanged)binding).PropertyChanged += UpdateProperty;
                        break;
                    case BindingMethod.Manual when manualUpdateMethod == ManualUpdateMethod.Periodical:
                        StartCoroutine(UpdateAllValuesPeriodicallyCoroutine());
                        break;
                }

                UpdateAll();
            }
        }

        private void Start()
        {
            if (!canBeAutoAssigned || !autoAssign)
                return;

            if (Binding == null)
                HookUsingAutoAssign();
        }

        private void OnDestroy()
        {
            listeners.Clear();

            if (binding != null && bindingMethod == BindingMethod.Dynamic)
                ((INotifyPropertyChanged)binding).PropertyChanged -= UpdateProperty;
            else if (updatePropertyEvent != null && bindingMethod == BindingMethod.Static)
                updatePropertyEvent.RemoveEventHandler(null, updatePropertyCall);
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (!canBeAutoAssigned)
                autoAssign = false;

            if (BindingType.IsStaticType())
                bindingMethod = BindingMethod.Static;
            else if (BindingType?.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }
#endif

        private void Update()
        {
            if (bindingMethod != BindingMethod.Manual || manualUpdateMethod != ManualUpdateMethod.EveryFrame)
                return;
            
            //We update only if binding method is manual and the update method is every frame.
            UpdateAll();
        }

        public static void InitializeStaticMembers()
        {
            if (initializedStatically)
                return;

            //Looking for UpdateProperty(string propertyName)
            updatePropertyMethod = typeof(BindingNode).GetMethods(~BindingFlags.Public)
                .First(x => x.Name == nameof(UpdateProperty) && x.GetParameters().Length == 1);

            initializedStatically = true;
        }

        public void InitializeStaticTypeListeners()
        {
            if (bindingMethod != BindingMethod.Static)
                return;

            updatePropertyCall = updatePropertyMethod.CreateDelegate(typeof(Action<string>), this);

            updatePropertyEvent =
                BindingType.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged),
                    BindingFlags.Static | BindingFlags.Public);

            if (updatePropertyEvent == null)
            {
                Debug.LogError(
                    $"\'event Action<string> PropertyChanged\' not found in static class {BindingType.Name}. Ensure that it is present to bind correctly. {gameObject.name}");
                return;
            }

            updatePropertyEvent.AddEventHandler(null, updatePropertyCall);
        }

        private void HookUsingAutoAssign() => Binding = GetComponent(BindingType);

        public void AddListener(BindingListenerBase listener, string getExpressionPath = "")
        {
            if (isUpdatingBindings)
                tempListeners.Add(new Tuple<BindingListenerBase, string>(listener, getExpressionPath));
            else
                listeners.Add(new Tuple<BindingListenerBase, string>(listener, getExpressionPath));

            if (BindingValid)
                listener.OnBindingUpdated(Binding);
        }

#if UNITY_EDITOR

        private void AssertTypeMatching(object obj)
        {
            Debug.Assert(BindingType == null || obj.GetType().IsAssignableFrom(BindingType), "Type mismatch");
        }
#endif

        private void UnhookPreviousObject()
        {
            switch (bindingMethod)
            {
                case BindingMethod.Dynamic:
                    ((INotifyPropertyChanged)binding).PropertyChanged -= UpdateProperty;
                    break;
                case BindingMethod.Manual when manualUpdateMethod == ManualUpdateMethod.Periodical:
                    StopCoroutine(UpdateAllValuesPeriodicallyCoroutine());
                    break;
            }
        }

        private void UpdateAll()
        {
            var obj = Binding;

            if (!BindingValid)
                return;

            isUpdatingBindings = true;

            foreach (var (listener, _) in listeners)
                listener.OnBindingUpdated(obj);

            while (tempListeners.Count > 0)
            {
                var newListeners = tempListeners.ToList();

                listeners.AddRange(newListeners);
                tempListeners.Clear();

                foreach (var (listener, _) in newListeners)
                    listener.OnBindingUpdated(obj);
            }

            isUpdatingBindings = false;
        }

        private void UpdateProperty(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName))
                UpdateAll();
            else
                UpdateProperty(e.PropertyName);
        }

        private void UpdateProperty(string propertyName)
        {
            var obj = Binding;

            if (!BindingValid || string.IsNullOrEmpty(propertyName))
                return;

            isUpdatingBindings = true;

            var newListeners = listeners;
            do
            {
                foreach (var (listener, propPath) in newListeners)
                {
                    if (propPath.Contains(propertyName))
                        listener.OnBindingUpdated(obj);
                }

                newListeners = tempListeners.ToList();
                listeners.AddRange(newListeners);

                tempListeners.Clear();
            } while (newListeners.Count > 0);

            isUpdatingBindings = false;
        }

        private IEnumerator UpdateAllValuesPeriodicallyCoroutine()
        {
            var wait = new WaitForSeconds(updateFrequencyInSeconds);
            while (isActiveAndEnabled)
            {
                UpdateAll();
                yield return wait;
            }
        }
    }
}