using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(BindingNode))]
    [CanEditMultipleObjects]
    public class BindingNodeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var boldLabel = new GUIStyle(EditorStyles.boldLabel);
            boldLabel.richText = true;

            var label = new GUIStyle(EditorStyles.label);
            label.richText = true;

            EditorGUILayout.Space();

            var obj = (BindingNode)target;
            var bindingTypeName = serializedObject.FindProperty("bindingTypeName").stringValue;
            var type = Type.GetType(bindingTypeName);

            if (string.IsNullOrEmpty(bindingTypeName))
            {
                EditorGUILayout.LabelField("No type selected. Define a type below.", SearchWindowsCommon.errorStyle);
            }
            else if (type == null)
            {
                var bindingNode = (BindingNode)target;

                bindingNode.TryFixingPath();
                EditorGUILayout.LabelField(
                    $"Type with full name \n<b>{bindingTypeName}</b>\n doesn't exist or is invalid. Change the type below.",
                    SearchWindowsCommon.errorStyle);
            }
            else
            {
                var bindingMethodProp = serializedObject.FindProperty("bindingMethod");
                var bindingMethod = (BindingMethod)bindingMethodProp.enumValueIndex;

                string bindingMethodStr = null;
                string bindingMethodNote = null;

                switch (bindingMethod)
                {
                    case BindingMethod.Dynamic:
                        bindingMethodStr = "<color=#22e05b>Bound Dynamically</color>";
                        break;
                    case BindingMethod.Manual:
                        bindingMethodStr = "<color=#514fc4>Bound Manually.</color>";
                        bindingMethodNote = "<b>Note :</b> Must be updated manually.";
                        break;
                    case BindingMethod.Static:
                        bindingMethodStr = "<color=cyan>Bound Statically.</color>";
                        bindingMethodNote =
                            "<b>Note :</b> Static class must implement event : <b>PropertyChanged</b>(string propertyName)";
                        break;
                }

                EditorGUILayout.LabelField($"<b>Type : <color=yellow>{type.Name}</color></b>", label);
                EditorGUILayout.LabelField(bindingMethodStr, boldLabel);

                if (!string.IsNullOrEmpty(bindingMethodNote))
                    EditorGUILayout.LabelField(bindingMethodNote, label);

                if (bindingMethod != BindingMethod.Static)
                    CheckAutoAssign(obj, "#22e05b");

                EditorGUILayout.PropertyField(serializedObject.FindProperty("performTypeChecks"));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Bound Object Type"))
            {
                var bindingNode = (BindingNode)target;
                bindingNode.TryFixingPath();
                BindingSearchWindow.Open(new BindingSearchCriteria(true), SetBindingType);
            }

            HandleSingletonAssignment(type);

            serializedObject.ApplyModifiedProperties();
        }

        private void CheckAutoAssign(BindingNode obj, string colorHex)
        {
            var isComponent = typeof(Component).IsAssignableFrom(obj.BindingType) &&
                obj.GetComponent(obj.BindingType);

            serializedObject.FindProperty("canBeAutoAssigned").boolValue = isComponent;

            if (isComponent)
            {
                EditorGUILayout.Space();
                var boldLabelWithRichText = new GUIStyle(EditorStyles.boldLabel);
                boldLabelWithRichText.richText = true;

                EditorGUILayout.LabelField(
                    $"Component found in GameObject, can be assigned <color={colorHex}>On Start</color>.",
                    boldLabelWithRichText);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAssign"));
            }
        }

        private void SetBindingType(BindingExpressionLocation bindingExpressionLocation,
            List<BindingPropertyEntry> bindingPropertyEntries)
        {
            serializedObject.FindProperty("bindingTypeName").stringValue =
                bindingPropertyEntries[0].AssemblyQualifiedTypeName;
            serializedObject.ApplyModifiedProperties();
        }


        private void HandleSingletonAssignment(Type type)
        {
            var boldLabelWithRichText = new GUIStyle(EditorStyles.boldLabel);
            boldLabelWithRichText.richText = true;

            var hasSingletons = HasSingletonInstance(type);
            var assignedViaSingletonProperty = serializedObject.FindProperty("assignedViaSingleton");

            var singletonGetProperty = serializedObject.FindProperty("singletonGetExpression");
            var singletonGetExpression = (BindingGetExpression)singletonGetProperty.boxedValue;

            if (!hasSingletons)
            {
                assignedViaSingletonProperty.boolValue = false;
                ((BindingNode)target).ClearSingletonExpression();
            }
            else
            {
                EditorGUILayout.LabelField("<color=green>Singleton detected!</color>", boldLabelWithRichText);
                EditorGUILayout.PropertyField(assignedViaSingletonProperty);
                ((BindingNode)target).InstantiateSingletonExpression();
            }

            if (!assignedViaSingletonProperty.boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("You can assign it here.", boldLabelWithRichText);
            EditorGUILayout.Space();

            if (GUILayout.Button("Set Singleton Instance"))
            {
                var searchCriteria = new BindingSearchCriteria(false);
                searchCriteria.BindingNodeType = type;
                searchCriteria.Location = BindingExpressionLocation.Static;
                searchCriteria.IsSingletonSearch = true;
                searchCriteria.CurrentAssemblyQualifiedTypeNames = new[] { type.AssemblyQualifiedName };
                searchCriteria.CurrentPath = type.Name;

                BindingSearchWindow.Open(searchCriteria,
                    (location, entries) =>
                        EditorDatabindingUtility.SetTargetPath(singletonGetProperty, location, entries));
            }

            var expressionValid = SingletonExpressionPathValid(type, singletonGetExpression.Path,
                singletonGetExpression.AssemblyQualifiedTypeNames);

            serializedObject.FindProperty("canBeAutoAssigned").boolValue =
                expressionValid.HasValue && expressionValid.Value;

            if (expressionValid == null)
                EditorGUILayout.LabelField("Path is not defined.", boldLabelWithRichText);
            else if (!expressionValid.Value)
                EditorGUILayout.LabelField(
                    "<color=red>Invalid Path</red> : Assign a Singleton Instance of the same type that is a valid member.",
                    boldLabelWithRichText);
            else if (expressionValid.Value)
                EditorGUILayout.LabelField($"Singleton Path : {singletonGetExpression.Path}", boldLabelWithRichText);
        }

        public static bool HasSingletonInstance(Type type)
        {
            if (type == null)
                return false;

            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            foreach (var field in type.GetFields(flags))
                if (field.FieldType == type)
                    return true;

            foreach (var property in type.GetProperties(flags))
                if (property.PropertyType == type && property.GetMethod != null)
                    return true;

            return false;
        }

        private bool? SingletonExpressionPathValid(Type bindingType, string path, string[] assemblyQualifiedTypeNames)
        {
            if (assemblyQualifiedTypeNames == null || assemblyQualifiedTypeNames.Length == 0 ||
                string.IsNullOrEmpty(path))
                return null;

            if (Type.GetType(assemblyQualifiedTypeNames[0]) != bindingType || assemblyQualifiedTypeNames.Length != 2)
                return false;

            var members = path.Split('.');
            var publicStaticMembers = bindingType.GetMembers(BindingFlags.Static | BindingFlags.Public);

            if (publicStaticMembers.Length == 0)
                return false;

            var member = publicStaticMembers.FirstOrDefault(x => x.Name == members[1]);
            return member != null && member.GetPropertyOrFieldType() == bindingType;
        }
    }
}
