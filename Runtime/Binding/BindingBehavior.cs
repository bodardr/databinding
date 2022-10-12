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
        public enum BindingMethod
        {
            Dynamic,
            Manual,
            Static
        }

        private static MethodInfo updatePropertyMethod;
        private static bool initializedStatically = false;

        [SerializeField]
        private bool autoAssign = true;

        [SerializeField]
        private bool canBeAutoAssigned = false;

        [SerializeField]
        private BindingMethod bindingMethod = BindingMethod.Static;

        [SerializeField]
        private string boundObjectTypeName = "";

        private readonly List<Tuple<BindingListenerBase, string>> listeners = new();

        /// <summary>
        /// List of nested listeners to be added after the binding update has finished.
        /// </summary>
        private readonly List<Tuple<BindingListenerBase, string>> tempListeners = new();

        private Type boundObjectType;
        private INotifyPropertyChanged dynamicallyBoundObject;

        private bool isUpdatingBindings = false;
        private object manuallyBoundObject;
        private Delegate updatePropertyCall;

        private EventInfo updatePropEvent;

        public bool IsObjectSet => BoundObject != null;

        private bool BoundObjectValid =>
            autoAssign || BoundObject != null || BoundObjectType.IsSealed && BoundObjectType.IsAbstract;

        public Type BoundObjectType
        {
            get => boundObjectType ??= Type.GetType(boundObjectTypeName);
            private set => boundObjectType = value;
        }

        private object BoundObject
        {
            get
            {
                if (bindingMethod != BindingMethod.Dynamic)
                    return manuallyBoundObject;

                if (autoAssign)
                    return dynamicallyBoundObject ??= HookUsingAutoAssign();

                return dynamicallyBoundObject;
            }
            set
            {
                if (bindingMethod == BindingMethod.Dynamic)
                    dynamicallyBoundObject = (INotifyPropertyChanged)value;
                else
                    manuallyBoundObject = value;

                UpdateAll();
            }
        }

        private void Start()
        {
            if (!canBeAutoAssigned || !autoAssign)
                return;

            HookUsingAutoAssign();
        }

        private void OnDestroy()
        {
            if (bindingMethod == BindingMethod.Dynamic)
                dynamicallyBoundObject.PropertyChanged -= UpdateProperty;
            else if (updatePropEvent != null && bindingMethod == BindingMethod.Static)
                updatePropEvent.RemoveEventHandler(null, updatePropertyCall);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!canBeAutoAssigned)
                autoAssign = false;

            if (BoundObjectType.IsStaticType())
                bindingMethod = BindingMethod.Static;
            else if (BoundObjectType?.GetInterface("INotifyPropertyChanged") != null)
                bindingMethod = BindingMethod.Dynamic;
            else
                bindingMethod = BindingMethod.Manual;
        }
#endif

        public static void InitializeStaticMembers()
        {
            if (initializedStatically)
                return;

            //Looking for UpdateProperty(string propertyName)
            updatePropertyMethod = typeof(BindingBehavior).GetMethods(~BindingFlags.Public)
                .First(x => x.Name == nameof(UpdateProperty) && x.GetParameters().Length == 1);

            initializedStatically = true;
        }

        public void InitializeStaticListeners()
        {
            if (bindingMethod != BindingMethod.Static)
                return;

            updatePropertyCall = updatePropertyMethod.CreateDelegate(typeof(Action<string>), this);

            updatePropEvent =
                BoundObjectType.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged),
                    BindingFlags.Static | BindingFlags.Public);

            if (updatePropEvent == null)
            {
                Debug.LogError(
                    $"\'event Action<string> PropertyChanged\' not found in static class {BoundObjectType.Name}. Ensure that it is present to bind correctly. {gameObject.name}");
                return;
            }

            updatePropEvent.AddEventHandler(null, updatePropertyCall);
        }

        private INotifyPropertyChanged HookUsingAutoAssign()
        {
            var obj = (INotifyPropertyChanged)GetComponent(BoundObjectType);
            obj.PropertyChanged += UpdateProperty;
            AssignNewObjectDynamic(obj);

            return obj;
        }

        public void AddListener(BindingListenerBase listener, string boundPropertyPath = "")
        {
            if (isUpdatingBindings)
                tempListeners.Add(new Tuple<BindingListenerBase, string>(listener, boundPropertyPath));
            else
                listeners.Add(new Tuple<BindingListenerBase, string>(listener, boundPropertyPath));

            if (BoundObjectValid)
                listener.UpdateValue(BoundObject);
        }

        public void SetValue<TDynamic>(TDynamic newBoundObject) where TDynamic : notnull, INotifyPropertyChanged
        {
            AssertTypeMatching<TDynamic>();

            UnhookPreviousObject();

            newBoundObject.PropertyChanged += UpdateProperty;
            bindingMethod = BindingMethod.Dynamic;

            AssignNewObject(newBoundObject);
        }

        public void SetValueManual<TManual>(TManual newBoundObject) where TManual : notnull
        {
            AssertTypeMatching<TManual>();

            UnhookPreviousObject();

            bindingMethod = BindingMethod.Manual;

            AssignNewObject(newBoundObject);
        }

        private void AssertTypeMatching<T>()
        {
            Debug.Assert(BoundObjectType == null || typeof(T).IsAssignableFrom(BoundObjectType), "Type mismatch");
        }

        private void UnhookPreviousObject()
        {
            if (bindingMethod == BindingMethod.Dynamic && dynamicallyBoundObject != null)
                dynamicallyBoundObject.PropertyChanged -= UpdateProperty;
        }

        private void AssignNewObject<T>(T newBoundObject) where T : notnull
        {
            BoundObjectType = typeof(T);
            BoundObject = newBoundObject;
        }

        /// <summary>
        /// Assigns the new object without specifying the type.
        /// </summary>
        private void AssignNewObjectDynamic(object newBoundObject)
        {
            BoundObjectType = newBoundObject.GetType();
            BoundObject = newBoundObject;
        }

        public void UpdateAll()
        {
            var obj = BoundObject;

            if (!BoundObjectValid)
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

        private void UpdateProperty(object sender, PropertyChangedEventArgs e) => UpdateProperty(e.PropertyName);

        private void UpdateProperty(string propertyName)
        {
            var obj = BoundObject;

            if (!BoundObjectValid || string.IsNullOrEmpty(propertyName))
                return;

            isUpdatingBindings = true;


            var newListeners = listeners;
            do
            {
                foreach (var (listener, propPath) in newListeners)
                {
                    if (propPath.Contains(propertyName))
                        listener.UpdateValue(obj);
                }

                newListeners = tempListeners.ToList();
                listeners.AddRange(newListeners);

                tempListeners.Clear();
            }
            while (newListeners.Count > 0);

            isUpdatingBindings = false;
        }
    }
}