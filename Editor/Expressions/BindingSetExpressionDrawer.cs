using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Editor
{
    [CustomPropertyDrawer(typeof(BindingSetExpression), true)]
    public class BindingSetExpressionDrawer : PropertyDrawer
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

            BindingInspectorCommon.DrawLabel("Destination", property, labelRect);

            if (GUI.Button(buttonRect, EditorGUIUtility.IconContent("editicon.sml")))
            {
                var searchCriteria = new BindingSearchCriteria(property);
                searchCriteria.Flags = BindingSearchCriteria.PropertyFlag.Setter;

                //By default, setters are in GameObject
                if (searchCriteria.Location == BindingExpressionLocation.None)
                    searchCriteria.Location = BindingExpressionLocation.InGameObject;

                BindingSearchWindow.Open(searchCriteria,
                    (location, members) => EditorDatabindingUtility.SetTargetPath(property, location, members));
            }
        }
    }
}
