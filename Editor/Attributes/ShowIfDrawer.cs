using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bodardr.Databinding.Editor
{

    [CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
    public class ShowIfDrawer : PropertyDrawer
    {
        private bool show;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateShow(property);
            return show ? base.GetPropertyHeight(property, label) : 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdateShow(property);

            if (show)
                EditorGUI.PropertyField(position, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var boolProp = property.FindSiblingProperty(((ShowIfAttribute)attribute).MemberName);
            
            container.Add(new PropertyField(property));
            container.TrackPropertyValue(boolProp,
                prop => UpdateContainerVisibility(container, prop));
            
            UpdateContainerVisibility(container, boolProp);

            return container;
        }
        private void UpdateContainerVisibility(VisualElement container, SerializedProperty prop)
        {
            var att = (ShowIfAttribute)attribute;
            var doShow = prop.boolValue;
            
            if(att.Invert)
                doShow = !doShow;
            
            container.style.display = doShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateShow(SerializedProperty property)
        {
            var att = (ShowIfAttribute)attribute;

            var prop = property.FindSiblingProperty(att.MemberName);
            var s = prop.boolValue;

            if (att.Invert)
                s = !s;

            show = s;
        }
    }
}
