﻿using System;
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

        private static GUIStyle searchResultStyle;
        private static GUIStyle richTextStyle;

        private static Texture2D blueTex;

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

            searchResultStyle = new GUIStyle
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


        public static void DisplayDoneButton(EditorWindow window)
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Done"))
                window.Close();

            EditorGUILayout.Space();
        }

        public static bool DisplaySearchBar(ref string searchQuery)
        {
            EditorGUILayout.Space(8);

            var diff = searchQuery;
            GUI.SetNextControlName("Search Query");
            searchQuery = EditorGUILayout.TextField(searchQuery);

            EditorGUILayout.Space(8);

            return !diff.Equals(searchQuery);
        }

        public static bool DisplayHeader(bool displayBackButton, string headerText)
        {
            var backButtonClicked = false;

            if (displayBackButton)
                backButtonClicked = GUILayout.Button("<", GUILayout.MaxWidth(20));

            GUILayout.Label($"<b>{headerText}</b>", richTextStyle);

            return backButtonClicked;
        }

        public static void DisplaySearchResults(List<string> searchResults, ref Vector2 scrollbarValue,
            Action<string> clickCallback)
        {
            GUILayout.BeginVertical();
            scrollbarValue = GUILayout.BeginScrollView(scrollbarValue, false, true);

            foreach (var property in searchResults)
                if (GUILayout.Button(property, searchResultStyle))
                    clickCallback(property);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}