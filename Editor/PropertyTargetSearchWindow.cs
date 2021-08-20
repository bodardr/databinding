using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public class PropertyTargetSearchWindow : EditorWindow
    {
        private string searchQuery = "";
        private string propertyPath = "";

        private Action<string> onComplete;
        private Vector2 scrollbarValue;

        private readonly Stack<Type> typeFrom = new Stack<Type>();
        private List<Component> components = new List<Component>(4);

        private GameObject gameObject;

        private Type propertyType;
        private IEnumerable<MemberInfo> memberInfos;

        private List<string> searchResults = new List<string>();
        private List<string> filteredResults = new List<string>();

        public string PropertyPath
        {
            get => propertyPath;
            set
            {
                propertyPath = value;

                UpdatePropertyList();
                UpdateSearchResults();

                onComplete.Invoke(PropertyPath);
            }
        }

        public static void Popup(string targetPath, GameObject gameObject, Action<string> onComplete)
        {
            var window = GetWindow<PropertyTargetSearchWindow>();

            window.gameObject = gameObject;
            window.onComplete = onComplete;
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

        private void OnGUI()
        {
            if (SearchWindowsCommon.DisplaySearchBar(ref searchQuery))
                UpdateSearchResults();

            GUILayout.BeginHorizontal(SearchWindowsCommon.headerStyle);

            if (SearchWindowsCommon.DisplayHeader(typeFrom.Count > 0, $"{gameObject.name}.{PropertyPath}"))
            {
                typeFrom.Pop();

                var indexOf = PropertyPath.LastIndexOf('.');

                if (indexOf > 0)
                    PropertyPath = PropertyPath.Substring(0, indexOf);
            }

            if (typeFrom.Count > 0)
                SearchWindowsCommon.DisplayDoneButton(this);

            GUILayout.EndHorizontal();

            if (typeFrom.Count > 0 && searchResults.Count < 1 && !typeFrom.Peek().IsPrimitive)
                GUILayout.Label("<b>No search results</b>", SearchWindowsCommon.noResultStyle);
            else
                SearchWindowsCommon.DisplaySearchResults(filteredResults, ref scrollbarValue, OnPropertyClicked);

            EditorGUILayout.Space();

            SearchWindowsCommon.DisplayDoneButton(this);
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
            }

            typeFrom.Push(reflectedType);

            if (typeFrom.Count <= 1)
                PropertyPath += name;
            else
                PropertyPath += '.' + name;
        }
    }
}