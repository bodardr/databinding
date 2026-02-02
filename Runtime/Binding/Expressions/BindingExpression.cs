using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{


    [Serializable]
    public abstract class BindingExpression<TExpr> : IBindingExpression where TExpr : Delegate
    {
        public static Dictionary<string, TExpr> Expressions { get; } = new();

        [SerializeField] protected string path;
        [SerializeField] protected string[] assemblyQualifiedTypeNames = Array.Empty<string>();

        protected Component component;
        private TExpr compiledExpression;

        protected TExpr ResolvedExpression
        {
            get
            {
                if (compiledExpression != null)
                    return compiledExpression;

                if (Expressions.TryGetValue(Path, out var val))
                    compiledExpression = val;
#if !ENABLE_IL2CPP || UNITY_EDITOR
                else
                    JITCompile(null);
#endif

                return compiledExpression;
            }
            set => compiledExpression = value;
        }

        public string[] AssemblyQualifiedTypeNames => assemblyQualifiedTypeNames;

        public string Path
        {
            get => path;
            set => path = value;
        }
        
#if !ENABLE_IL2CPP || UNITY_EDITOR
        public bool ShouldCompile(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> expressionsToCompile, bool fromAot)
        {
            var type = GetType();
            
            if (!expressionsToCompile.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<string, Tuple<IBindingExpression, GameObject>>();
                expressionsToCompile.Add(type, dict);
            }

            return !dict.ContainsKey(Path) && (fromAot || !Expressions.ContainsKey(Path));
        }
        public abstract void JITCompile(GameObject context);
#endif

#if UNITY_EDITOR
        public abstract string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries);

        public abstract bool IsValid(BindingListenerBase context, BindingNode bindingNode,
            out BindingExpressionErrorContext errorCtx);
#endif

        protected void ThrowExpressionError(GameObject compilationContext, Exception e)
        {
            if (compilationContext != null)
                UnityDispatcher.EnqueueOnUnityThread(() =>
                    Debug.LogError(
                        $"<b>Databinding</b> : Error compiling {compilationContext.name}'s <b>{Path}</b> : {e}",
                        compilationContext));
            else
                UnityDispatcher.EnqueueOnUnityThread(() =>
                    Debug.LogError($"<b>Databinding</b> : Error compiling with <b>{Path}</b> : {e}"));
        }
    }

}
