using System;
using System.Collections.Generic;
using UnityEngine;
namespace Bodardr.Databinding.Runtime
{
    public interface IBindingExpression
    {
        public string Path { get; set; }

        public void JITCompile(GameObject context);
        
        #if UNITY_EDITOR
        public string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries);

        public bool IsValid(GameObject context, BindingNode bindingNode, out BindingExpressionErrorContext errorCtx);
        #endif
    }
}
