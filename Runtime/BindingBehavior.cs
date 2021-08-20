using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

// ReSharper disable PossibleMultipleEnumeration
namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Behavior")]
    public class BindingBehavior : MonoBehaviour
    {
        private Type boundObjectType;

        [SerializeField]
        private string boundObjectTypeName = typeof(TestDataClass).AssemblyQualifiedName;

        private INotifyPropertyChanged boundObject;

        private readonly Dictionary<string, BindingListener> listeners = new Dictionary<string, BindingListener>();

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

        private void OnValidate()
        {
            boundObjectType = Type.GetType(boundObjectTypeName);
        }

        public void AddListener(string boundPropertyPath, BindingListener action)
        {
            listeners.Add(boundPropertyPath, action);

            //todo : if deeper in hierarchy (and is NotifyOnPropertyChanged), subscribe to the event too.
        }

        public void SetValue<T>(T newBoundObject) where T : INotifyPropertyChanged
        {
            if (typeof(T) != boundObjectType)
                Debug.LogError($"Type mismatch - Tried to assign {nameof(T)} to {boundObjectType.Name}");

            if (boundObject != null)
                boundObject.PropertyChanged -= UpdateBindings;

            boundObject = newBoundObject;

            if (boundObject == null)
                return;

            BoundObjectType = typeof(T);
            boundObject.PropertyChanged += UpdateBindings;

            foreach (var listener in listeners)
                listener.Value.UpdateValue(boundObject);
        }

        private void UpdateBindings(object sender, PropertyChangedEventArgs e)
        {
            var value = BoundObjectType.GetProperty(e.PropertyName)?.GetValue(sender);
            var correspondingListeners = listeners.Where(x => x.Key == e.PropertyName);

            foreach (var keyValuePair in correspondingListeners)
                keyValuePair.Value.UpdateValue(value);
        }
    }
}