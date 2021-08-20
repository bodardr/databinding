#define BDR_DATABINDING

using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    public class BindableExpressionCompiler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CompileAllExpressions()
        {
            var listeners = Resources.FindObjectsOfTypeAll<BindingListener>();

            foreach (var listener in listeners)
                listener.Initialize();
        }
    }
}