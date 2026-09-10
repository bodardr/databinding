//This is a special edge case for Odin because this package has a specific ShowIf attribute
#if ODIN_INSPECTOR
using Bodardr.Databinding.Runtime;
using UnityEditor;

namespace Bodardr.Databinding.Editor
{
    [CustomEditor(typeof(BindingCollectionBehavior))]
    public class BindingCollectionBehaviorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI() => DrawDefaultInspector();
    }
}
#endif