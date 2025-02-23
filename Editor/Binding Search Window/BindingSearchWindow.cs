﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Bodardr.Databinding.Editor;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;

public class BindingSearchWindow : EditorWindow
{
    private const int maxSearchResults = 250;

    /// <summary>
    /// The member stack which represents the binding path.
    /// The first element in the list is not a member, but the inputType
    /// </summary>
    private Stack<BindingPropertyEntry> memberStack;
    private BindingSearchCriteria searchCriteria;

    private List<BindingPropertyEntry> memberList;
    private List<BindingPropertyEntry> searchResults;

    private Vector2 scrollbarValue;
    private Action<BindingExpressionLocation, List<BindingPropertyEntry>> updateCallback;

    private string bindingPath;
    private string searchInput = string.Empty;
    private bool isLastMemberValid;
    private BindingExpressionLocation location;

    private GUIStyle searchResultTypeStyle;
    private GUIStyle searchResultStyle;
    private bool isTypeOnly;
    public BindingSearchWindow()
    {
        isTypeOnly = searchCriteria.TypeOnly;
    }

    private bool IsWindowInvalid => memberStack == null || updateCallback == null;

    public bool CanShowBackButton =>
        !isTypeOnly && (memberStack.Count > 1 ||
            searchCriteria.Location != BindingExpressionLocation.InBindingNode && memberStack.Count > 0);


    public BindingExpressionLocation Location
    {
        get => location;
        set
        {
            if (location == value)
                return;

            location = value;
            UpdateSearchLocation(location);
        }
    }

    public static void Open(BindingSearchCriteria searchCriteria,
        Action<BindingExpressionLocation, List<BindingPropertyEntry>> updateCallback)
    {
        var window = GetWindow<BindingSearchWindow>(true, "Binding - Assign Property", true);
        window.InitializeWindow(searchCriteria, updateCallback);
    }

    private void InitializeWindow(BindingSearchCriteria searchCriteria,
        Action<BindingExpressionLocation, List<BindingPropertyEntry>> updateCallback)
    {
        this.searchCriteria = searchCriteria;
        this.updateCallback = updateCallback;

        searchResultStyle = new GUIStyle(EditorStyles.label)
        {
            padding = new RectOffset(8, 0, 8, 8),
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            richText = true,
        };

        searchResultTypeStyle = new GUIStyle(EditorStyles.label)
        {
            padding = new RectOffset(0, 8, 8, 8),
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Italic,
            richText = true,
        };

        isTypeOnly = searchCriteria.TypeOnly;
        memberStack = EditorDatabindingUtility.ParseExistingPath(searchCriteria.CurrentPath,
            searchCriteria.CurrentAssemblyQualifiedTypeNames);
        memberList = new();
        searchResults = new();
        location = this.searchCriteria.Location;
        bindingPath = string.Empty;
        scrollbarValue = Vector2.one;

        if (memberStack.Count < 1 || searchCriteria.Location == BindingExpressionLocation.InBindingNode &&
            (searchCriteria.BindingNode == null || memberStack.Last().Type != searchCriteria.BindingNode.BindingType))
        {
            UpdateSearchLocation(searchCriteria.Location);
        }
        else
        {
            searchInput = string.Empty;
            UpdateMemberList();
            UpdateSearchResults();
        }

        bindingPath = memberStack.Reverse().ToList().PrintPath();
    }

    private void OnGUI()
    {
        if (IsWindowInvalid)
        {
            Close();
            return;
        }

        if (SearchWindowsCommon.DrawHeader(CanShowBackButton,
            string.IsNullOrEmpty(bindingPath) ? "Undefined" : bindingPath))
            OnBackClicked();

        Location = SearchWindowsCommon.DrawSearchBar(location, isTypeOnly, ref searchInput, out var hasInputChanged);

        if (hasInputChanged)
            UpdateSearchResults();

        //Display List of Properties
        GUILayout.BeginVertical();
        scrollbarValue = GUILayout.BeginScrollView(scrollbarValue, false, true);

        foreach (var entry in searchResults)
        {
            if (!DrawEntry(entry))
                continue;

            OnPropertyClicked(entry);
            break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        SearchWindowsCommon.DrawDoneAndCancel(isLastMemberValid, out var cancelClicked, out var doneClicked);

        if (doneClicked)
            updateCallback(Location, memberStack.Reverse().ToList());

        if (doneClicked || cancelClicked)
            Close();
    }

    private bool DrawEntry(BindingPropertyEntry propertyEntry)
    {
        var buttonRect = EditorGUILayout.BeginHorizontal();
        var hasClicked = GUI.Button(buttonRect, GUIContent.none);

        if (isTypeOnly)
        {
            var displayName = propertyEntry.DisplayName;

            if (propertyEntry.IsDynamic)
                displayName = "(<color=#22e05b><b>Dynamic</b></color>)\t" + displayName;
            else if (propertyEntry.IsStatic)
                displayName = "(<color=cyan><b>Static</b></color>)\t" + displayName;

            GUILayout.Label(displayName, searchResultStyle);
        }
        else
        {
            GUILayout.Label(propertyEntry.DisplayName, searchResultStyle);
        }

        GUILayout.Label(propertyEntry.TypeOnly ? propertyEntry.Type.Namespace : propertyEntry.Type.Name,
            searchResultTypeStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndVertical();
        return hasClicked;
    }

    private void OnPropertyClicked(BindingPropertyEntry propertyEntry)
    {
        //If we're only searching for a type, we have to pop the previous one
        if (isTypeOnly)
            memberStack.Clear();

        memberStack.Push(propertyEntry);
        bindingPath = memberStack.Reverse().ToList().PrintPath();

        //If we're not only searching for a type, we have to update the member list
        if (!isTypeOnly)
            UpdateMemberList();
        else
            isLastMemberValid = EvaluateIfLastMemberValid();
    }
    private void OnBackClicked()
    {
        memberStack.Pop();
        UpdateMemberList();
        bindingPath = memberStack.Reverse().ToList().PrintPath();
    }

    private void UpdateMemberList()
    {
        isLastMemberValid = EvaluateIfLastMemberValid();

        memberList.Clear();
        bool isTypeList = isTypeOnly || memberStack.Count < 1;

        memberList = isTypeList ? GetTypes(searchCriteria) : GetMembers(searchCriteria, memberStack.Peek());

        searchInput = string.Empty;
        UpdateSearchResults();
    }

    private bool EvaluateIfLastMemberValid()
    {
        if (memberStack.Count < 1)
            return false;

        if (isTypeOnly)
            return memberStack.Count == 1;

        switch (memberStack.Peek().MemberInfo)
        {
            case FieldInfo field:
                return !searchCriteria.Flags.HasFlag(BindingSearchCriteria.PropertyFlag.Setter) || !field.IsInitOnly;
            case PropertyInfo prop:
                MethodInfo[] accessors = prop.GetAccessors();
                if (searchCriteria.Flags.HasFlag(BindingSearchCriteria.PropertyFlag.Getter) &&
                    accessors.All(y => y.ReturnType != prop.PropertyType))
                    return false;

                return !searchCriteria.Flags.HasFlag(BindingSearchCriteria.PropertyFlag.Setter) ||
                    accessors.Any(y => y.ReturnType == typeof(void));

            default:
                return location == BindingExpressionLocation.InBindingNode;
        }
    }

    private void UpdateSearchResults()
    {
        //todo : use a better search system.
        searchResults =
            memberList.AsParallel()
                .Where(x => x.Name.Contains(searchInput, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(x => x.IsDynamic ? 0 : 1)
                .Take(maxSearchResults).OrderBy(x => x.IsDynamic ? 0 : 1)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Name.Length).ToList();
    }

    private List<BindingPropertyEntry> GetTypes(BindingSearchCriteria searchCriteria)
    {
        switch (searchCriteria.Location)
        {
            case BindingExpressionLocation.InGameObject:
                var gameObject = searchCriteria.TargetGO;
                if (gameObject == null)
                    Debug.LogError("No GameObject has been defined in the search criteria.");
                return gameObject.GetComponents<Component>().Select(x => new BindingPropertyEntry(x.GetType()))
                    .ToList();
            case BindingExpressionLocation.None:
            case BindingExpressionLocation.Static:
            case BindingExpressionLocation.InBindingNode:
                return TypeExtensions.TypeCache.AsParallel().Select(x => new BindingPropertyEntry(x)).ToList();
        }

        return null;
    }

    private List<BindingPropertyEntry> GetMembers(BindingSearchCriteria searchCriteria,
        BindingPropertyEntry propertyFrom)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        //Static members only show up for the second member of the hierarchy. 
        if (memberStack.Count == 1 && searchCriteria.Location == BindingExpressionLocation.Static)
            bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        return propertyFrom.Type.FindFieldsAndProperties(bindingFlags)
            .Select(x => new BindingPropertyEntry(x.GetPropertyOrFieldType(), x.Name, x)).ToList();
    }

    public void UpdateSearchLocation(BindingExpressionLocation newLocation)
    {
        searchCriteria.Location = newLocation;
        memberStack.Clear();
        searchResults.Clear();

        if (newLocation is BindingExpressionLocation.InBindingNode &&
            searchCriteria.BindingNode != null && searchCriteria.BindingNode.BindingType != null)
            memberStack.Push(new BindingPropertyEntry(searchCriteria.BindingNode.BindingType));

        bindingPath = memberStack.Reverse().ToList().PrintPath();
        UpdateMemberList();
        UpdateSearchResults();
    }
}
