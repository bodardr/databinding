using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public class GenericSerializedObject : ISerializationCallbackReceiver
    {
        [SerializeField]
        private object value = null;

        [SerializeField]
        private Object objectRef;

        [SerializeField]
        private string json;

        [SerializeField]
        private string typeStr;

        [SerializeField]
        private SerializationType serializationType = SerializationType.Json;

        public object Value
        {
            get => serializationType == SerializationType.Object ? objectRef : value;
            set
            {
                if (value is Object o)
                {
                    serializationType = SerializationType.Object;
                    objectRef = o;
                }
                else
                {
                    serializationType = SerializationType.Json;
                    this.value = value;
                }
            }
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (value == null || serializationType == SerializationType.Object || value.GetType().IsAssignableFrom(typeof(Object)))
                return;

            var type = value.GetType();

            if (serializationType == SerializationType.Json)
            {
                if (type == typeof(string) || type.IsPrimitive)
                    json = value.ToString();
                else
                    json = JsonUtility.ToJson(value);
            }

            typeStr = type.AssemblyQualifiedName;
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (serializationType == SerializationType.Object || string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(typeStr))
                return;

            var type = Type.GetType(typeStr);

            if (type == typeof(string))
                value = json;
            else
                value = type.IsPrimitive ? Convert.ChangeType(json, type) : JsonUtility.FromJson(json, type);
#endif
        }
    }

    public enum SerializationType
    {
        Json,
        Object
    }
}