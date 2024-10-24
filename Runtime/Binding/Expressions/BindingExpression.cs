using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{


    [Serializable]
    public abstract class BindingExpression<TExpr> : IBindingExpression where TExpr : Delegate
    {
        public static HashSet<ExpressionEntry<TExpr>> Expressions { get; } = new(new ExpressionEntryComparer<TExpr>());

        [SerializeField] protected string path;
        [SerializeField] protected string[] assemblyQualifiedTypeNames;

        protected Component component;
        private TExpr compiledExpression;

        protected TExpr ResolvedExpression
        {
            get
            {
                if (compiledExpression != null)
                    return compiledExpression;

                if (Expressions.TryGetValue(new ExpressionEntry<TExpr>(Path), out ExpressionEntry<TExpr> val))
                    compiledExpression = val.Expression;
                else
                    JITCompile(null);

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

        public abstract void JITCompile(GameObject context);

        #if UNITY_EDITOR
        public bool IsCompiled => compiledExpression != null || Expressions.Contains(new(Path));

        public abstract string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries);

        public abstract bool IsValid(GameObject context, BindingNode bindingNode,
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
