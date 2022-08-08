using System;
using System.Collections.Generic;
using System.Linq;
using Bodardr.Utility.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public class BoundTypeSearchWindow : EditorWindow
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
            var window = GetWindow<BoundTypeSearchWindow>();

            window.titleContent = new GUIContent("Search Property");
            window.onComplete = onComplete;
            window.objectTypeName = typeName;
            window.UpdateSearchResults();
            window.ShowPopup();
        }

        private static void InitializePropertyList()
        {
            notifyPropList = TypeExtensions.AllTypes.Where(x => x.GetInterface("INotifyPropertyChanged") != null)
                .Distinct().ToList();
            staticTypesList = TypeExtensions.AllTypes.Where(x => x.IsStaticType());
            otherTypesList = TypeExtensions.AllTypes.Except(notifyPropList).Distinct().ToList();
        }

        private void OnGUI()
        {
            if (SearchWindowsCommon.DisplaySearchBar(ref searchQuery))
                UpdateSearchResults();

            if (!string.IsNullOrEmpty(objectTypeName))
            {
                GUILayout.BeginHorizontal(SearchWindowsCommon.headerStyle);
                SearchWindowsCommon.DisplayHeader(false, objectTypeName);
                GUILayout.EndHorizontal();
            }


            if (notifyPropResults != null && otherTypesResults != null && !notifyPropResults.Any() &&
                !staticTypeResults.Any() && !otherTypesResults.Any())
                GUILayout.Label("<b>No search results</b>", SearchWindowsCommon.errorStyle);
            else
                DisplaySearchResults();

            EditorGUILayout.Space();

            SearchWindowsCommon.DisplayDoneButton(this);
        }

        protected virtual void UpdateSearchResults()
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
                if (!GUILayout.Button($"<color=yellow>{property.Name}</color>", boldSearchResultStyle))
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