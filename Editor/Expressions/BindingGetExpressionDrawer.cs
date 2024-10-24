using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomPropertyDrawer(typeof(BindingGetExpression), true)]
    public class BindingGetExpressionDrawer : PropertyDrawer
    {
        private const float buttonWidth = 25;

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var labelRect = new Rect(position);
            labelRect.width = position.width - buttonWidth;

            position.x += labelRect.width;
            position.width -= labelRect.width;
            var buttonRect = new Rect(position);
            
            BindingInspectorCommon.DrawLabel("Source", property, labelRect);

            if (GUI.Button(buttonRect, EditorGUIUtility.IconContent("editicon.sml")))
            {
                var searchCriteria = new BindingSearchCriteria(property);
                searchCriteria.Location = searchCriteria.BindingNode == null ? BindingExpressionLocation.Static
                    : BindingExpressionLocation.InBindingNode;
                searchCriteria.Flags = BindingSearchCriteria.PropertyFlag.Getter;

                BindingSearchWindow.Open(searchCriteria,
                    (location, entries) => EditorDatabindingUtility.SetTargetPath(property, location, entries));
            }
        }
    }
}
