﻿using System;
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
            if (value == null || serializationType == SerializationType.Object ||
                value.GetType().IsAssignableFrom(typeof(Object)))
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
        }

        public void OnAfterDeserialize()
        {
            if (serializationType == SerializationType.Object || string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(typeStr))
                return;

            var type = Type.GetType(typeStr);

            if (type == typeof(string))
                value = json;
            else if (type == typeof(bool))
                value = string.Equals(json, "True", StringComparison.InvariantCultureIgnoreCase);
            else
                value = type.IsPrimitive ? Convert.ChangeType(json, type) : JsonUtility.FromJson(json, type);
        }
    }

    public enum SerializationType
    {
        Json,
        Object
    }
}