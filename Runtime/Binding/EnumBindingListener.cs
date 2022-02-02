using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;
#endif


namespace Bodardr.Databinding.Runtime
{
    public class EnumBindingListener : BindingListener
    {
        [HideInInspector]
        [SerializeField]
        private GenericSerializedObject[] values;

#if UNITY_EDITOR
        [MenuItem("CONTEXT/Image/Databinding - Add Enum Listener")]
        public new static void AddImageListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<EnumBindingListener>();
            
            bindingListener.SetExpression.Path = "Image.sprite";
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[0] = typeof(Image).AssemblyQualifiedName;
            bindingListener.SetExpression.AssemblyQualifiedTypeNames[1] = typeof(Sprite).AssemblyQualifiedName;
        }
#endif
        
        public override void UpdateValue(object obj)
        {
            var enumIndex = (int)GetExpression.Expression(obj);
            SetExpression.Expression(component, values[enumIndex].Value);
        }
    }
}