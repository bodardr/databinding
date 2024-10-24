using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public class PropertyTargetSearchWindow : EditorWindow
    {
        private readonly Stack<Type> typeFrom = new();
        private List<Component> components = new(4);
        private List<string> filteredResults = new();
        private GameObject gameObject;

        private IEnumerable<MemberInfo> memberInfos;

        private Action<SerializedObject, string, Type[]> onComplete;
        private Action<SerializedObject, Type> onComponentSet;
        private string propertyPath = "";

        private Type propertyType;

        private Vector2 scrollbarValue;
        private string searchQuery = "";
        private List<string> searchResults = new();

        private SerializedObject serializedObject;

        public string PropertyPath
        {
            get => propertyPath;
            set
            {
                propertyPath = value;

                UpdatePropertyList();
                UpdateSearchResults();

                onComplete.Invoke(serializedObject, PropertyPath, typeFrom.Reverse().ToArray());
            }
        }

        private void OnGUI()
        {
            if (SearchWindowsCommon.DrawSearchBar(ref searchQuery))
                UpdateSearchResults();

            GUILayout.BeginHorizontal(SearchWindowsCommon.headerStyle);

            if (SearchWindowsCommon.DrawHeader(typeFrom.Count > 0, $"{gameObject.name}.{PropertyPath}"))
            {
                typeFrom.Pop();

                var indexOf = PropertyPath.LastIndexOf('.');

                PropertyPath = indexOf > 0 ? PropertyPath.Substring(0, indexOf) : "";
            }

            if (typeFrom.Count > 0)
                SearchWindowsCommon.DrawDoneButton();

            GUILayout.EndHorizontal();

            if (typeFrom.Count > 0 && searchResults.Count < 1 && !typeFrom.Peek().IsPrimitive)
                GUILayout.Label("<b>No search results</b>", SearchWindowsCommon.errorStyle);
            else
                SearchWindowsCommon.DisplaySearchResults(filteredResults, ref scrollbarValue, OnPropertyClicked);

            EditorGUILayout.Space();

            SearchWindowsCommon.DrawDoneButton();
        }

        public static void Popup(SerializedObject serializedObject, string targetPath, GameObject gameObject,
            Action<SerializedObject, string, Type[]> onComplete,
            Action<SerializedObject, Type> onComponentSet)
        {
            var window = GetWindow<PropertyTargetSearchWindow>();

            window.serializedObject = serializedObject;
            window.gameObject = gameObject;
            window.onComplete = onComplete;
            window.onComponentSet = onComponentSet;
            window.titleContent = new GUIContent("Property Target Search");

            window.components = gameObject.GetComponents(typeof(Component)).ToList();

            window.ParsePath(targetPath);
            window.UpdatePropertyList();
            window.UpdateSearchResults();
            window.ShowPopup();
        }

        private void ParsePath(string targetPath)
        {
            //todo : todo.
        }

        private void UpdatePropertyList()
        {
            if (typeFrom.Count < 1)
            {
                searchResults = components.Select(x => x.GetType().Name).ToList();
            }
            else
            {
                var type = typeFrom.Peek();
                memberInfos = type.FindFieldsAndProperties();
                searchResults = memberInfos.Select(x => x.Name).ToList();
            }
        }

        protected virtual void UpdateSearchResults()
        {
            filteredResults = searchResults.Where(x => x.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        protected virtual void OnPropertyClicked(string name)
        {
            searchQuery = "";

            Type reflectedType;

            if (typeFrom.Count > 0)
            {
                var memberInfo = memberInfos.Single(x => x.Name.Equals(name));

                if (memberInfo.MemberType == MemberTypes.Property)
                    reflectedType = ((PropertyInfo)memberInfo).PropertyType;
                else
                    reflectedType = ((FieldInfo)memberInfo).FieldType;
            }
            else
            {
                reflectedType = components.Find(x => x.GetType().Name == name).GetType();
                onComponentSet(serializedObject, reflectedType);
            }

            typeFrom.Push(reflectedType);

            if (typeFrom.Count <= 1)
                PropertyPath = name;
            else
                PropertyPath += '.' + name;
        }
    }
}