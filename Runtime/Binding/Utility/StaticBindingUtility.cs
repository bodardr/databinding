using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
namespace Bodardr.Databinding.Runtime
{
    public static class StaticBindingUtility
    {
        private static readonly Dictionary<Type, EventInfo> updatePropertyEvents = new();

        public static void SubscribeToPropertyChangedStatic(Type staticType, bool subscribe, Delegate del)
        {
            if (!updatePropertyEvents.TryGetValue(staticType, out var updatePropertyEvent))
            {
                updatePropertyEvent = staticType.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged),
                    BindingFlags.Static | BindingFlags.Public);

                if (updatePropertyEvent == null)
                {
                    Debug.LogWarning(
                        $"\'event Action<object, PropretyChangedEventArgs> PropertyChanged\' not found in static class {staticType.Name}. Ensure that it is present to bind correctly.");
                    return;
                }
                
                updatePropertyEvents[staticType] = updatePropertyEvent;
            }

            if (subscribe)
                updatePropertyEvent.AddEventHandler(null, del);
            else
                updatePropertyEvent.RemoveEventHandler(null, del);
        }
    }
}
