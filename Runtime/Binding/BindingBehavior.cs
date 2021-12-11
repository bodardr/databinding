using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

// ReSharper disable PossibleMultipleEnumeration
namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Behavior")]
    public class BindingBehavior : MonoBehaviour
    {
        delegate void PropCallback(string propertyName);

        [SerializeField]
        private BindingMethod bindingMethod = BindingMethod.Static;

        [SerializeField]
        private string boundObjectTypeName = typeof(TestDataClass).AssemblyQualifiedName;

        private Type boundObjectType;
        private INotifyPropertyChanged notifyObject;
        private object boundObject;

        private readonly List<Tuple<BindingListenerBase, string>> listeners = new();
        
        private EventInfo updatePropEvent;
        private Delegate updatePropertyDelegate;

        public bool IsObjectSet => boundObject != null;

        public bool BoundObjectInvalid =>
            BoundObject == null && !(BoundObjectType.IsSealed && BoundObjectType.IsAbstract);

        public Type BoundObjectType
        {
            get
            {
                if (boundObjectType == null)
                    OnValidate();

                return boundObjectType;
            }
            private set => boundObjectType = value;
        }

        private object BoundObject
        {
            get => bindingMethod == BindingMethod.Dynamic ? notifyObject : boundObject;
            set
            {
                if (bindingMethod == BindingMethod.Dynamic)
                    notifyObject = (INotifyPropertyChanged)value;
                else
                    boundObject = value;
            }
        }

        private void OnValidate()
        {
            boundObjectType = Type.GetType(boundObjectTypeName);

            if (boundObjectType.IsAbstract && boundObjectType.IsSealed)
                bindingMethod = BindingMethod.Static;
            else if (boundObjectType.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }

        public void InitializeStaticListeners()
        {
            if (bindingMethod != BindingMethod.Static)
                return;

            var type = GetType();

            var updatePropMethod = type.GetMethod("UpdateProperty");
            updatePropertyDelegate = updatePropMethod.CreateDelegate(typeof(Action<string>), this);

            updatePropEvent =
                BoundObjectType.GetEvent("OnPropertyChanged", BindingFlags.Static | BindingFlags.Public);

            if (updatePropEvent == null)
            {
                Debug.LogError(
                    $"\'event Action<string> OnPropertyChanged\' not found in static class {type.Name}. Ensure that it is present to bind correctly. {gameObject.name}");
                return;
            }

            updatePropEvent.AddEventHandler(null, updatePropertyDelegate);
        }

        private void OnDestroy()
        {
            if (updatePropEvent != null && bindingMethod == BindingMethod.Static)
                updatePropEvent.RemoveEventHandler(null, updatePropertyDelegate);
        }

        public void AddListener(BindingListenerBase listener, string boundPropertyPath = "")
        {
            //todo : if deeper in hierarchy (and is NotifyOnPropertyChanged), subscribe to the event too.
            listeners.Add(new Tuple<BindingListenerBase, string>(listener, boundPropertyPath));

            if (!BoundObjectInvalid)
                listener.UpdateValue(BoundObject);
        }

        public void SetValue<T>(T newBoundObject) where T : notnull, INotifyPropertyChanged
        {
            Debug.Assert(BoundObjectType.IsAssignableFrom(typeof(T)), "Type mismatch");

            UnhookPreviousObject();

            newBoundObject.PropertyChanged += UpdateBindings;
            bindingMethod = BindingMethod.Dynamic;
            AssignNewObject(newBoundObject);
        }

        public void SetValueManual<T>(T newBoundObject) where T : notnull
        {
            Debug.Assert(boundObjectType == null || typeof(T).IsAssignableFrom(boundObjectType), $"Type mismatch : {typeof(T).Name} and {newBoundObject.GetType().Name}");

            UnhookPreviousObject();
            bindingMethod = BindingMethod.Manual;
            AssignNewObject(newBoundObject);
        }

        private void UnhookPreviousObject()
        {
            if (bindingMethod == BindingMethod.Dynamic && notifyObject != null)
                notifyObject.PropertyChanged -= UpdateBindings;
        }

        private void AssignNewObject<T>(T newBoundObject) where T : notnull
        {
            BoundObject = newBoundObject;
            BoundObjectType = typeof(T);

            UpdateAll();
        }

        private void UpdateBindings(object sender, PropertyChangedEventArgs e) => UpdateProperty(e.PropertyName);

        public void UpdateAll()
        {
            var obj = BoundObject;

            if (BoundObjectInvalid)
                return;

            foreach (var listener in listeners)
                listener.Item1.UpdateValue(obj);
        }

        public void UpdateProperty(string propertyName)
        {
            var obj = BoundObject;

            if (BoundObjectInvalid)
                return;

            if (string.IsNullOrEmpty(propertyName))
            {
                UpdateAll();
                return;
            }

            foreach (var (listener, propPath) in listeners)
            {
                if (propPath.Contains(propertyName))
                    listener.UpdateValue(obj);
            }
        }

        public enum BindingMethod
        {
            Dynamic,
            Manual,
            Static
        }
    }
}