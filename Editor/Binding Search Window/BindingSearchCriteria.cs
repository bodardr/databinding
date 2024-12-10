using System;
using Bodardr.Databinding.Editor;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

public struct BindingSearchCriteria
{
    [Flags]
    public enum PropertyFlag
    {
        None = default,
        Getter,
        Setter
    }

    public GameObject TargetGO { get; set; }
    public BindingNode BindingNode { get; set; }
    public BindingExpressionLocation Location { get; set; }

    public PropertyFlag Flags { get; set; }

    public string CurrentPath { get; set; }
    public string[] CurrentAssemblyQualifiedTypeNames { get; set; }

    public BindingSearchCriteria(SerializedProperty property)
    {
        var path = property.FindPropertyRelative("path");
        var assemblyQualifiedTypes = property.FindPropertyRelative("assemblyQualifiedTypeNames");

        BindingNode = property.serializedObject.FindProperty("bindingNode").objectReferenceValue as BindingNode;

        var enumValues = Enum.GetValues(typeof(BindingExpressionLocation));
        Location = (BindingExpressionLocation)enumValues.GetValue(property.FindPropertyRelative("location")
            .enumValueIndex);
        TargetGO = ((Component)property.serializedObject.targetObject).gameObject;
        CurrentPath = path.stringValue;
        CurrentAssemblyQualifiedTypeNames = (string[])assemblyQualifiedTypes.GetValue();
        Flags = PropertyFlag.None;
    }
}
