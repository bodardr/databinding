﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public sealed class BindingTypeSearchWindow : EditorWindow
    {
        private const int maxResults = 50;
        private string searchQuery = "";
        private string objectTypeName;

        private static GUIStyle boldSearchResultStyle;
        private static Texture2D blueTex;

        private static IEnumerable<Type> notifyPropList;
        private static IEnumerable<Type> staticTypesList;
        private static IEnumerable<Type> otherTypesList;

        private IEnumerable<Type> notifyPropResults;
        private IEnumerable<Type> staticTypeResults;
        private IEnumerable<Type> otherTypesResults;

        private Type selectedType;

        private Action<string> onComplete;

        private Vector2 scrollbarValue = Vector2.zero;

        private void OnEnable()
        {
            blueTex = new Texture2D(1, 1);
            blueTex.SetPixel(0, 0, new Color(0f, 0.59f, 1f));
            blueTex.Apply();

            var selectedState = new GUIStyleState { background = blueTex, textColor = Color.white };

            boldSearchResultStyle = new GUIStyle
            {
                normal = EditorStyles.label.normal,
                active = selectedState,
                onHover = selectedState,
                alignment = TextAnchor.MiddleLeft,
                focused = selectedState,
                padding = new RectOffset(16, 16, 8, 8)
            };

            InitializePropertyList();
        }

        public static void Popup(string typeName, Action<string> onComplete)
        {
            var window = GetWindow<BindingTypeSearchWindow>();

            window.titleContent = new GUIContent("Search Property");
            window.onComplete = onComplete;
            window.objectTypeName = typeName;
            window.UpdateSearchResults();
            window.ShowPopup();
        }

        private static void InitializePropertyList()
        {
            notifyPropList = TypeExtensions.TypeCache.Where(x => x.GetInterface(nameof(INotifyPropertyChanged)) != null)
                .Distinct().ToList();
            staticTypesList = TypeExtensions.TypeCache.Where(x => x.IsSealed && x.IsAbstract);
            otherTypesList = TypeExtensions.TypeCache.Except(notifyPropList).Distinct().ToList();
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(objectTypeName))
                SearchWindowsCommon.DrawHeader(false, objectTypeName);
            
            if (SearchWindowsCommon.DrawSearchBar(ref searchQuery))
                UpdateSearchResults();
            
            if (notifyPropResults != null && otherTypesResults != null && !notifyPropResults.Any() &&
                !staticTypeResults.Any() && !otherTypesResults.Any())
                GUILayout.Label("<b>No search results</b>", SearchWindowsCommon.errorStyle);
            else
                DisplaySearchResults();

            EditorGUILayout.Space();
            
            if (SearchWindowsCommon.DrawDoneButton())
                Close();
        }

        private void UpdateSearchResults()
        {
            notifyPropResults = notifyPropList
                .Where(x => x.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).Take(maxResults)
                .ToList();

            var count = notifyPropResults.Count();

            staticTypeResults = staticTypesList
                .Where(x => x.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(maxResults - count)
                .ToList();

            count += staticTypeResults.Count();

            if (count < maxResults)
                otherTypesResults = otherTypesList
                    .Where(x => x.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(maxResults - count).ToList();
        }

        private void DisplaySearchResults()
        {
            GUILayout.BeginVertical();
            scrollbarValue = GUILayout.BeginScrollView(scrollbarValue, false, true);

            foreach (var property in notifyPropResults)
            {
                if (!GUILayout.Button($"<color=yellow>{property?.Name}</color>", boldSearchResultStyle))
                    continue;

                onComplete(property.AssemblyQualifiedName);
                objectTypeName = property.FullName;
            }

            foreach (var property in staticTypeResults)
            {
                if (!GUILayout.Button($"<color=cyan>{property.Name}</color>", boldSearchResultStyle))
                    continue;

                onComplete(property.AssemblyQualifiedName);
                objectTypeName = property.FullName;
            }

            if (otherTypesResults != null)
                foreach (var property in otherTypesResults)
                {
                    if (!GUILayout.Button(property.Name, boldSearchResultStyle))
                        continue;

                    onComplete(property.AssemblyQualifiedName);
                    objectTypeName = property.FullName;
                }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}