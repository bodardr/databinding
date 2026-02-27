using System;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public class GenericSerializedObject : ISerializationCallbackReceiver
    {
        private object value = null;

        [SerializeField]
        private Object objectRef;

        [SerializeField]
        private string json;

        [FormerlySerializedAs("typeStr")]
        [SerializeField]
        private string assemblyQualifiedTypeName;

        public object Value
        {
            get => value ?? objectRef;
            set
            {
                if (value is Object o)
                {
                    objectRef = o;
                    this.value = null;
                }
                else
                {
                    this.value = value;
                    objectRef = null;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            if (value == null || objectRef != null)
                return;

            var type = value.GetType();

            if (type == typeof(string) || type.IsPrimitive)
                json = value.ToString();
            else
                json = JsonUtility.ToJson(value);

            assemblyQualifiedTypeName = type.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(assemblyQualifiedTypeName))
                return;

            var type = Type.GetType(assemblyQualifiedTypeName);

            if (type == typeof(string))
                value = json;
            else if (type == typeof(bool))
                value = string.Equals(json, "True", StringComparison.InvariantCultureIgnoreCase);
            else if (type != null)
                value = type.IsPrimitive ? Convert.ChangeType(json, type) : JsonUtility.FromJson(json, type);
        }
    }
}
