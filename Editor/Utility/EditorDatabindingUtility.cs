using System;
using System.Collections.Generic;
using Bodardr.Databinding.Editor;
using Bodardr.Databinding.Runtime;
using UnityEditor;

public static class EditorDatabindingUtility
{
    public static Stack<BindingPropertyEntry> ParseExistingPath(string path, string[] assemblyQualifiedTypeNames)
    {
        if (string.IsNullOrEmpty(path) || assemblyQualifiedTypeNames == null || assemblyQualifiedTypeNames.Length < 1)
            return new Stack<BindingPropertyEntry>();

        var output = new Stack<BindingPropertyEntry>();

        var splitPath = path.Split('.');

        var currentType = Type.GetType(assemblyQualifiedTypeNames[0]);
        output.Push(new BindingPropertyEntry(currentType));
        for (int i = 1; i < splitPath.Length; i++)
        {
            var members = currentType.GetMember(splitPath[i]);

            //If the member couldn't be found.
            if (members.Length < 1)
                return output;

            var member = members[0];
            var memberType = member.GetPropertyOrFieldType();

            //If the member type couldn't be found.
            if (memberType == null)
                return output;

            output.Push(new BindingPropertyEntry(memberType, member.Name, member));
            currentType = memberType;
        }

        return output;
    }

    public static void SetTargetPath(SerializedProperty prop, BindingExpressionLocation location,
        List<BindingPropertyEntry> entries)
    {
        if (prop.serializedObject.targetObject != null)
            Undo.RecordObject(prop.serializedObject.targetObject, "Applied Binding Path");

        var locationProp = prop.FindPropertyRelative("location");
        var enumValues = Enum.GetValues(typeof(BindingExpressionLocation));
        locationProp.enumValueIndex = Array.IndexOf(enumValues, location);

        var getPath = prop.FindPropertyRelative("path");
        getPath.stringValue = entries.PrintPath();

        var array = prop.FindPropertyRelative("assemblyQualifiedTypeNames");
        array.arraySize = entries.Count;

        var i = 0;
        foreach (SerializedProperty element in array)
        {
            element.stringValue = entries[i].AssemblyQualifiedTypeName;
            i++;
        }

        prop.serializedObject.ApplyModifiedProperties();
    }

    public static void SetExpressionPathManually(BindingListenerBase listener, UnityEngine.Object context,
        string expressionPropertyName,
        List<BindingPropertyEntry> properties, BindingExpressionLocation location)
    {
        var serializedObject = new SerializedObject(listener, context);

        SetTargetPath(
            serializedObject.FindProperty(expressionPropertyName),
            location,
            properties);
    }
}
