using System;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public class GenericSerializedObject : ISerializationCallbackReceiver
    {
        [SerializeReference]
        private object value = null;

        [SerializeField]
        private string strValue = null;

        [SerializeField]
        private Object objectRef;

        [SerializeField]
        [Obsolete]
        private string json;

        [FormerlySerializedAs("typeStr")]
        [SerializeField]
        [Obsolete]
        private string assemblyQualifiedTypeName;

        [SerializeField]
        private SerializationType serializationType = SerializationType.Json;

        public object Value
        {
            get => value ?? (string.IsNullOrEmpty(strValue) ? objectRef : strValue);
            set
            {
                if (value is Object o)
                {
                    this.value = null;
                    objectRef = o;
                    strValue = null;
                }
                else if (value is string s)
                {
                    this.value = null;
                    objectRef = null;
                    strValue = s;
                }
                else
                {
                    this.value = value;
                    objectRef = null;
                    strValue = null;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            //Nothing to do anymore. It is now automatically serialized in the value
            //variable.
        }

        public void OnAfterDeserialize()
        {
            if (serializationType == SerializationType.Object || string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(assemblyQualifiedTypeName))
                return;

            var type = Type.GetType(assemblyQualifiedTypeName);

            if (type == typeof(string))
                Value = json;
            else if (type == typeof(bool))
                Value = string.Equals(json, "True", StringComparison.InvariantCultureIgnoreCase);
            else if (type != null)
                Value = type.IsPrimitive ? Convert.ChangeType(json, type) : JsonUtility.FromJson(json, type);

            json = string.Empty;
            assemblyQualifiedTypeName = string.Empty;
        }
    }

    public enum SerializationType
    {
        Json,
        Object
    }
}
