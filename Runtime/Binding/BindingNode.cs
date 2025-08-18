using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;

// ReSharper disable PossibleMultipleEnumeration
namespace Bodardr.Databinding.Runtime
{
    public enum BindingMethod
    {
        Dynamic,
        Manual,
        Static
    }

    [AddComponentMenu("Databinding/Binding Node")]
    public class BindingNode : MonoBehaviour, INotifyPropertyChanged
    {
        private readonly List<BindingListenerBase> listeners = new();
        private readonly List<BindingListenerBase> listenersToRemove = new();
        private readonly List<BindingListenerBase> listenersToAdd = new();

        private Type bindingType;
        private object binding;

        private bool isUpdatingBindings = false;

        private Delegate updatePropertyCall;
        private EventInfo updatePropertyEvent;

        private static MethodInfo updatePropertyMethod;
        private static bool initializedStatically = false;

        [SerializeField]
        private bool performTypeChecks = true;

        [SerializeField]
        private bool autoAssign = true;

        [SerializeField]
        private bool canBeAutoAssigned = false;

        [SerializeField]
        private BindingMethod bindingMethod = BindingMethod.Static;

        [SerializeField]
        private string bindingTypeName = "";

        public Type BindingType
        {
            get
            {
                if (bindingType == null || !bindingType.Name.Equals(bindingTypeName))
                    bindingType = Type.GetType(bindingTypeName);

                return bindingType;
            }
        }

        public object Binding
        {
            get => binding;
            set
            {
                if (binding != null)
                    UnhookPreviousObject();

                if (performTypeChecks && value != null && !AssertTypeMatching(value))
                {
                    Debug.LogError(
                        $"BindingNode : Expected Type {BindingType.Name}. Actual Type : {value.GetType().Name}.\nIf this isn't intended, you can disable type checks by setting {nameof(performTypeChecks)} to false.",
                        gameObject);
                    binding = null;
                }
                else
                {
                    binding = value;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Binding)));

                if (IsAssigned && BindingMethod == BindingMethod.Dynamic)
                    ((INotifyPropertyChanged)binding).PropertyChanged += UpdateProperty;

                isUpdatingBindings = true;
                UpdateAll();
                isUpdatingBindings = false;
            }
        }

        public BindingMethod BindingMethod => bindingMethod;

        public bool IsAssigned => BindingMethod == BindingMethod.Static || binding != default;

        public event PropertyChangedEventHandler PropertyChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeStaticMembers()
        {
            if (initializedStatically)
                return;

            //Looking for UpdateProperty(string propertyName)
            updatePropertyMethod = typeof(BindingNode).GetMethods(~BindingFlags.Public)
                .First(x => x.Name == nameof(UpdateProperty) && x.GetParameters().Length == 1);

            initializedStatically = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (BindingType != null && BindingType.IsAbstract && BindingType.IsSealed)
                bindingMethod = BindingMethod.Static;
            else if (BindingType?.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }

        public bool ValidateErrors()
        {
            var valid = Type.GetType(bindingTypeName) != null || !gameObject.scene.IsValid();

            if (!valid)
                Debug.LogError(
                    $"Couldn't find type from fully qualified name : {bindingTypeName}. Assign a valid type.",
                    gameObject);

            return valid;
        }
#endif

        private void Start()
        {
            if (!canBeAutoAssigned || !autoAssign)
                return;

            if (!IsAssigned)
                HookUsingAutoAssign();
        }
        private bool AssertTypeMatching(object value)
        {
            var type = value.GetType();
            var typeMatches = BindingType.IsAssignableFrom(type) || type.GetInterfaces().Contains(BindingType);
            return typeMatches;
        }

        private void OnDestroy()
        {
            listeners.Clear();

            if (binding != null)
                UnhookPreviousObject();
        }
        public void InitializeStaticTypeListeners()
        {
            if (BindingMethod != BindingMethod.Static)
                return;

            updatePropertyCall = updatePropertyMethod.CreateDelegate(typeof(Action<string>), this);

            updatePropertyEvent = BindingType.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged),
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

        public void AddListener(BindingListenerBase listener)
        {
            if (isUpdatingBindings)
                listenersToAdd.Add(listener);
            else
                listeners.Add(listener);
        }
        public void RemoveListener(BindingListenerBase listener)
        {
            if (isUpdatingBindings)
                listenersToRemove.Add(listener);
            else
                listeners.Remove(listener);
        }

        private void UnhookPreviousObject()
        {
            switch (BindingMethod)
            {
                case BindingMethod.Dynamic:
                    ((INotifyPropertyChanged)binding).PropertyChanged -= UpdateProperty;
                    break;
                case BindingMethod.Static:
                    if (updatePropertyEvent != null)
                        updatePropertyEvent.RemoveEventHandler(null, updatePropertyCall);
                    break;
            }
        }

        private void UpdateProperty(object sender, PropertyChangedEventArgs e)
        {
            var isNestedCall = isUpdatingBindings;
            isUpdatingBindings = true;

            if (string.IsNullOrWhiteSpace(e.PropertyName))
                UpdateAll();
            else
                UpdateProperty(e.PropertyName);

            if (isNestedCall)
                return;

            foreach (var listener in listenersToRemove)
                listeners.Remove(listener);
            listenersToRemove.Clear();

            listeners.AddRange(listenersToAdd);
            listenersToAdd.Clear();

            isUpdatingBindings = false;
        }

        private void UpdateAll()
        {
            if (!IsAssigned)
                return;

            Profiler.BeginSample("BindingNode.UpdateAll", this);

            var obj = Binding;
            foreach (var listener in listeners)
                listener.OnBindingUpdated(obj);

            Profiler.EndSample();
        }

        private void UpdateProperty(string propertyName)
        {
            if (!IsAssigned || string.IsNullOrEmpty(propertyName))
                return;

            Profiler.BeginSample("BindingNode.UpdateProperty", this);

            var obj = Binding;
            foreach (var listener in listeners)
                if (listener.GetExpression.Path.Contains(propertyName))
                    listener.OnBindingUpdated(obj);

            Profiler.EndSample();
        }

    }
}
