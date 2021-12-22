using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public class PropertySearchWindow : EditorWindow
    {
        private string searchQuery = "";

        private List<MemberInfo> searchResults = new List<MemberInfo>();
        private readonly Stack<Type> typeFrom = new Stack<Type>();
        private SerializedProperty serializedProperty;

        private List<MemberInfo> typeProperties;
        private Action<SerializedProperty, string, Type[]> onComplete;

        private string propertyPath;
        private Vector2 scrollbarValue;

        public string PropertyPath
        {
            get => propertyPath;
            set
            {
                propertyPath = value;

                UpdatePropertyList();
                UpdateSearchResults();

                var types = typeFrom.Reverse().ToArray();
                onComplete.Invoke(serializedProperty, PropertyPath, types);
            }
        }

        public static void Popup(SerializedProperty serializedObject, Type typeFrom, Action<SerializedProperty, string, Type[]> onComplete)
        {
            var window = GetWindow<PropertySearchWindow>();

            window.serializedProperty = serializedObject;
            window.typeFrom.Push(typeFrom);
            window.onComplete = onComplete;
            window.titleContent = new GUIContent("Search Property");
            window.propertyPath = typeFrom.Name;
            window.ShowPopup();
            window.UpdatePropertyList();
            window.UpdateSearchResults();
        }

        private void UpdatePropertyList()
        {
            typeProperties = typeFrom.Peek().FindFieldsAndProperties().ToList();
        }

        private void OnGUI()
        {
            if (SearchWindowsCommon.DisplaySearchBar(ref searchQuery))
                UpdateSearchResults();

            GUILayout.BeginHorizontal(SearchWindowsCommon.headerStyle);

            if (SearchWindowsCommon.DisplayHeaderWithThis(typeFrom.Count > 1, PropertyPath))
            {
                if (typeFrom.Count <= 1)
                {
                    PropertyPath = typeFrom.First().Name;
                }
                else
                {
                    typeFrom.Pop();
                    var indexOf = PropertyPath.LastIndexOf('.');
                    PropertyPath = indexOf > 0 ? PropertyPath.Substring(0, indexOf) : "";
                }
            }

            GUILayout.EndHorizontal();

            if (typeFrom.Count > 0 && searchResults.Count < 1 && !typeFrom.Peek().IsPrimitive)
                GUILayout.Label("<b>No search results</b>", SearchWindowsCommon.errorStyle);
            else
                SearchWindowsCommon.DisplaySearchResults(searchResults.Select(x => x.Name).ToList(), ref scrollbarValue,
                    OnPropertyClicked);

            EditorGUILayout.Space();

            SearchWindowsCommon.DisplayDoneButton(this);
        }

        protected virtual void UpdateSearchResults()
        {
            searchResults = typeProperties
                .Where(x => x.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            UpdatePropertyList();
        }

        protected virtual void OnPropertyClicked(string name)
        {
            var member = searchResults.Find(x => x.Name == name);

            searchQuery = "";

            var reflectedType = member.MemberType == MemberTypes.Field
                ? ((FieldInfo)member).FieldType
                : ((PropertyInfo)member).PropertyType;

            typeFrom.Push(reflectedType);
            PropertyPath += '.' + member.Name;
        }
    }
}