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
        private readonly List<Tuple<BindingListenerBase, string>> listeners = new();

        private Type bindingType;
        private object binding;

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

#if UNITY_EDITOR
                if (value != null)
                    AssertTypeMatching(value);
#endif

                binding = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Binding)));

                if (IsAssigned && BindingMethod == BindingMethod.Dynamic)
                    ((INotifyPropertyChanged)binding).PropertyChanged += UpdateProperty;

                UpdateAll();
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

        private void Start()
        {
            if (!canBeAutoAssigned || !autoAssign)
                return;

            if (!IsAssigned)
                HookUsingAutoAssign();
        }

#if UNITY_EDITOR

        private void AssertTypeMatching(object obj)
        {
            var type = obj.GetType();
            Debug.Assert(type.IsAssignableFrom(BindingType) || type.GetInterfaces().Contains(BindingType),
                "Type mismatch");
        }
        private void OnDestroy()
        {
            listeners.Clear();

            if (binding != null)
                UnhookPreviousObject();
        }
        private void OnValidate()
        {
            if (!canBeAutoAssigned)
                autoAssign = false;

            if (BindingType != null && BindingType.IsAbstract && BindingType.IsSealed)
                bindingMethod = BindingMethod.Static;
            else if (BindingType?.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }
#endif
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

        public void AddListener(BindingListenerBase listener, string path = "")
        {
            listeners.Add(new Tuple<BindingListenerBase, string>(listener, path));

            if (IsAssigned)
                listener.OnBindingUpdated(Binding);
        }
        public void RemoveListener(BindingListenerBase listener, string path = "")
        {
            var listenerFound = listeners.Find(x =>
                x.Item1 == listener && x.Item2.Equals(path));

            if (listenerFound != null)
                listeners.Remove(listenerFound);
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

        private void UpdateAll()
        {
            Profiler.BeginSample("BindingNode.UpdateAll", this);

            var obj = Binding;

            foreach (var (listener, _) in listeners)
                listener.OnBindingUpdated(obj);

            Profiler.EndSample();
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
            Profiler.BeginSample("BindingNode.UpdateProperty", this);

            var obj = Binding;

            if (!IsAssigned || string.IsNullOrEmpty(propertyName))
                return;

            foreach (var (listener, propPath) in listeners)
            {
                if (propPath.Contains(propertyName))
                    listener.OnBindingUpdated(obj);
            }

            Profiler.EndSample();
        }
    }
}
