using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Bodardr.Utility.Runtime;
using UnityEngine;

// ReSharper disable PossibleMultipleEnumeration
namespace Bodardr.Databinding.Runtime
{
    [AddComponentMenu("Databinding/Binding Behavior")]
    public class BindingBehavior : MonoBehaviour
    {
        private static MethodInfo updatePropertyMethod;
        private static bool initializedStatically = false;

        delegate void PropCallback(string propertyName);

        private Type boundObjectType;
        private INotifyPropertyChanged notifyObject;
        private object boundObject;

        private readonly List<Tuple<BindingListenerBase, string>> listeners = new();

        /// <summary>
        /// Listeners to be added after the update is finished. 
        /// </summary>
        private readonly List<Tuple<BindingListenerBase, string>> tempListeners = new();

        private EventInfo updatePropEvent;
        private Delegate updatePropertyDelegate;

        private bool isUpdatingBindings = false;

        [SerializeField]
        private bool autoAssign = true;

        [SerializeField]
        private bool canBeAutoAssigned = false;

        [SerializeField]
        private BindingMethod bindingMethod = BindingMethod.Static;

        [SerializeField]
        private string boundObjectTypeName = typeof(TestDataClass).AssemblyQualifiedName;

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

            if (boundObjectType.IsStatic())
                bindingMethod = BindingMethod.Static;
            else if (boundObjectType.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }

        public static void InitializeStaticMembers()
        {
            if (initializedStatically)
                return;

            var type = typeof(BindingBehavior);
            updatePropertyMethod = type.GetMethod("UpdateProperty");

            initializedStatically = true;
        }

        public void InitializeStaticListeners()
        {
            if (bindingMethod != BindingMethod.Static)
                return;

            updatePropertyDelegate = updatePropertyMethod.CreateDelegate(typeof(Action<string>), this);

            updatePropEvent =
                BoundObjectType.GetEvent("OnPropertyChanged", BindingFlags.Static | BindingFlags.Public);

            if (updatePropEvent == null)
            {
                Debug.LogError(
                    $"\'event Action<string> OnPropertyChanged\' not found in static class {BoundObjectType.Name}. Ensure that it is present to bind correctly. {gameObject.name}");
                return;
            }

            updatePropEvent.AddEventHandler(null, updatePropertyDelegate);
        }

        private void Awake()
        {
            if (!canBeAutoAssigned || !autoAssign)
                return;

            UnhookPreviousObject();

            var obj = (INotifyPropertyChanged)GetComponent(BoundObjectType);
            obj.PropertyChanged += UpdateBindings;
            AssignNewObjectDynamic(obj);
        }

        private void OnDestroy()
        {
            if (updatePropEvent != null && bindingMethod == BindingMethod.Static)
                updatePropEvent.RemoveEventHandler(null, updatePropertyDelegate);
        }

        public void AddListener(BindingListenerBase listener, string boundPropertyPath = "")
        {
            if (isUpdatingBindings)
                tempListeners.Add(new Tuple<BindingListenerBase, string>(listener, boundPropertyPath));
            else
                listeners.Add(new Tuple<BindingListenerBase, string>(listener, boundPropertyPath));

            if (!BoundObjectInvalid)
                listener.UpdateValue(BoundObject);
        }

        public void SetValue<T>(T newBoundObject) where T : notnull, INotifyPropertyChanged
        {
            Debug.Assert(BoundObjectType == null || typeof(T).IsAssignableFrom(BoundObjectType), "Type mismatch");

            UnhookPreviousObject();

            newBoundObject.PropertyChanged += UpdateBindings;
            bindingMethod = BindingMethod.Dynamic;
            AssignNewObject(newBoundObject);
        }

        public void SetValueManual<T>(T newBoundObject) where T : notnull
        {
            Debug.Assert(BoundObjectType == null || typeof(T).IsAssignableFrom(BoundObjectType),
                $"Type mismatch : {typeof(T).Name} and {BoundObjectType.Name}");

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

        /// <summary>
        /// Assigns the new object without specifying the type.
        /// </summary>
        /// <param name="newBoundObject">The new databound object</param>
        private void AssignNewObjectDynamic(object newBoundObject)
        {
            BoundObject = newBoundObject;

            UpdateAll();
        }

        private void UpdateBindings(object sender, PropertyChangedEventArgs e) => UpdateProperty(e.PropertyName);

        public void UpdateAll()
        {
            var obj = BoundObject;

            if (BoundObjectInvalid)
                return;

            isUpdatingBindings = true;
            
            foreach (var (listener, _) in listeners)
                listener.UpdateValue(obj);

            while (tempListeners.Count > 0)
            {
                var newListeners = tempListeners.ToList();
                
                listeners.AddRange(newListeners);
                tempListeners.Clear();
                
                foreach (var (listener, _) in newListeners)
                    listener.UpdateValue(obj);
            }

            isUpdatingBindings = false;
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

            isUpdatingBindings = true;

            foreach (var (listener, propPath) in listeners)
            {
                if (propPath.Contains(propertyName))
                    listener.UpdateValue(obj);
            }
            
            while (tempListeners.Count > 0)
            {
                var newListeners = tempListeners.ToList();
                
                listeners.AddRange(newListeners);
                tempListeners.Clear();
                
                foreach (var (listener, propPath) in listeners)
                {
                    if (propPath.Contains(propertyName))
                        listener.UpdateValue(obj);
                }
            }

            isUpdatingBindings = false;
        }

        public enum BindingMethod
        {
            Dynamic,
            Manual,
            Static
        }
    }
}