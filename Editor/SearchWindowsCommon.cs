using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    public static class SearchWindowsCommon
    {
        private static bool init = false;

        internal static GUIStyle errorStyle;
        internal static GUIStyle headerStyle;

        private static GUIStyle richTextStyle;

        private static Texture2D blueTex;
        public static GUIStyle SearchResultStyle;

        static SearchWindowsCommon()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (init)
                return;

            blueTex = new Texture2D(1, 1);
            blueTex.SetPixel(0, 0, new Color(0f, 0.59f, 1f));
            blueTex.Apply();

            var selectedState = new GUIStyleState { background = blueTex, textColor = Color.white };
            var boldLabel = EditorStyles.boldLabel;
            var defaultStyle = boldLabel.normal;

            SearchResultStyle = new GUIStyle
            {
                normal = defaultStyle,
                active = selectedState,
                onHover = selectedState,
                alignment = TextAnchor.MiddleLeft,
                focused = selectedState,
                padding = new RectOffset(16, 16, 8, 8)
            };

            errorStyle = new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.red },
                padding = new RectOffset(16, 16, 8, 8),
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            headerStyle = new GUIStyle
            {
                normal = new GUIStyleState { background = Texture2D.grayTexture, textColor = Color.white },
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 16, 16)
            };

            richTextStyle = new GUIStyle
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                normal = defaultStyle
            };

            init = true;
        }

        public static bool DrawHeader(bool displayBackButton, string headerText)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            var backButtonClicked = false;
            if (displayBackButton)
                backButtonClicked = GUILayout.Button("<", GUILayout.MaxWidth(20));

            GUILayout.Label($"<b>{headerText}</b>", richTextStyle);
            EditorGUILayout.EndHorizontal();

            return backButtonClicked;
        }

        public static bool DrawHeaderWithThis(bool displayBackButton, string headerText)
        {
            bool click;
            if (displayBackButton)
                click = GUILayout.Button("<", GUILayout.MaxWidth(20));
            else
                click = GUILayout.Button("this", GUILayout.MaxWidth(45));

            GUILayout.Label($"<b>{headerText}</b>", richTextStyle);
            return click;
        }
        public static bool DrawSearchBar(ref string searchQuery)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            var diff = searchQuery;
            GUI.SetNextControlName("Search Query");
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            return !diff.Equals(searchQuery);
        }

        public static BindingExpressionLocation DrawSearchBar(in BindingExpressionLocation location,
            ref string searchQuery, out bool hasSearchChanged)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            var newLocation =
                (BindingExpressionLocation)EditorGUILayout.EnumPopup(location, GUILayout.ExpandWidth(false));

            var diff = searchQuery;
            GUI.SetNextControlName("BindingSearchBar");
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));
            hasSearchChanged = !diff.Equals(searchQuery);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
            GUI.FocusControl("BindingSearchBar");

            return newLocation;
        }

        public static void DisplaySearchResults(List<string> searchResults, ref Vector2 scrollbarValue,
            Action<string> clickCallback)
        {
            GUILayout.BeginVertical();
            scrollbarValue = GUILayout.BeginScrollView(scrollbarValue, false, true);

            foreach (var property in searchResults)
                if (GUILayout.Button(property, SearchResultStyle))
                    clickCallback(property);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        public static bool DrawDoneButton()
        {
            EditorGUILayout.Space();
            var buttonClicked = GUILayout.Button("Done");
            EditorGUILayout.Space();

            return buttonClicked;
        }

        public static void DrawDoneAndCancel(bool isLastMemberValid, out bool cancelClicked, out bool doneClicked)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            cancelClicked = GUILayout.Button("Cancel", GUILayout.ExpandWidth(false));

            GUI.enabled = isLastMemberValid;
            doneClicked = GUILayout.Button("Done", GUILayout.ExpandWidth(false));
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }
}
