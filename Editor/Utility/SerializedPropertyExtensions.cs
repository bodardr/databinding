using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    public static class SerializedPropertyExtensions
    {
        private const string arrayDataPath = ".Array.data";
        
        public static object GetValue(this SerializedProperty prop)
        {
            var path = prop.propertyPath;
            var splitPath = path.Split('.').ToList();

            for (int i = splitPath.Count - 2; i >= 0; i--)
            {
                if (i > 0 && splitPath[i].Contains("Array") && splitPath[i + 1].Contains("data["))
                {
                    var arrayCharIndexorStart = splitPath[i + 1].IndexOf('[');
                    //We append the index to the split path.
                    splitPath[i - 1] += splitPath[i + 1][arrayCharIndexorStart..];

                    //We remove the Array and data[] entries.
                    splitPath.RemoveAt(i + 1);
                    splitPath.RemoveAt(i);
                }
            }

            object obj = prop.serializedObject.targetObject;

            for (int i = 0; i < splitPath.Count; i++)
            {
                var s = splitPath[i];

                var arrayCharIndexorStart = splitPath[i].IndexOf('[');
                var isArray = arrayCharIndexorStart >= 0;

                if (isArray)
                    s = s.Remove(arrayCharIndexorStart);

                var field = obj.GetType()
                    .GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(x => x.Name.Equals(s));

                if (isArray)
                {
                    var arrayIndex = int.Parse(splitPath[i][(arrayCharIndexorStart + 1)..^1]);
                    var collection = field.GetValue(obj) as IList;
                    obj = collection[arrayIndex];
                }
                else
                {
                    obj = field.GetValue(obj);
                }
            }
            return obj;
        }

        public static SerializedProperty FindParent(this SerializedProperty prop)
        {
            if (prop.depth < 1)
            {
                throw new Exception("Cannot use 'GetParent' on root property");
            }

            var propPath = prop.propertyPath;
            return prop.serializedObject.FindProperty(propPath[..propPath.LastIndexOf('.')]);
        }

        public static SerializedProperty FindSiblingProperty(this SerializedProperty prop, string relativePropertyPath)
        {
            return prop.depth > 0
                ? prop.FindParent().FindPropertyRelative(relativePropertyPath)
                : prop.serializedObject.FindProperty(relativePropertyPath);
        }
    }
}
