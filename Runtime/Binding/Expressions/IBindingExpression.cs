using System;
using System.Collections.Generic;
using UnityEngine;
namespace Bodardr.Databinding.Runtime
{
    public interface IBindingExpression
    {
        public string Path { get; set; }

#if !ENABLE_IL2CPP || UNITY_EDITOR
        public void JITCompile(GameObject context);
#endif

#if UNITY_EDITOR
        public string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries);

        public bool IsValid(BindingListenerBase context, BindingNode bindingNode,
            out BindingExpressionErrorContext errorContext);
#endif
    }
}
