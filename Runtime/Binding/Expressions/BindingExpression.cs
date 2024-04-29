using System;
using System.Collections.Generic;
using Bodardr.Utility.Runtime;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    [Serializable]
    public abstract class BindingExpression<D> : IBindingExpression where D : Delegate
    {
        [SerializeField]
        protected string path;

        [SerializeField]
        protected string[] assemblyQualifiedTypeNames = new string[2];

        public D Expression
        {
            get
            {
#if UNITY_EDITOR
                if (!CompiledExpressions.ContainsKey(path))
                    Compile(null);
#endif
                return CompiledExpressions[path];
            }
        }

        public string[] AssemblyQualifiedTypeNames => assemblyQualifiedTypeNames;

        #if UNITY_EDITOR
        public bool ExpressionAlreadyCompiled => CompiledExpressions != null && CompiledExpressions.ContainsKey(Path);
        #endif

        protected abstract Dictionary<string, D> CompiledExpressions { get; }

        public string Path
        {
            get => path;
            set => path = value;
        }

        #if UNITY_EDITOR
        public abstract void Compile(GameObject compilationContext);
        public abstract string PreCompile(out HashSet<string> usings, List<Tuple<string, string>> getters, List<Tuple<string, string>> setters);
        #endif

            #if UNITY_EDITOR
        public void ResolveExpression(GameObject context)
        {
            if (Expression != null)
                return;

            if (!ExpressionAlreadyCompiled)
                Compile(context);
        }
            #endif

        protected void ThrowExpressionError(GameObject compilationContext, Exception e)
        {
            if (compilationContext)
                UnityDispatcher.EnqueueOnUnityThread(() =>
                    Debug.LogError($"<b>Databinding</b> : Error compiling {compilationContext.name}'s <b>{Path}</b> : {e}", compilationContext));
            else
                UnityDispatcher.EnqueueOnUnityThread(() =>
                    Debug.LogError($"<b>Databinding</b> : Error compiling with <b>{Path}</b> : {e}"));
        }
    }

    public interface IBindingExpression
    {
        public string Path { get; }

        #if UNITY_EDITOR
        public void Compile(GameObject compilationContext);
        public string PreCompile(out HashSet<string> usings, List<Tuple<string, string>> getters, List<Tuple<string, string>> setters);
        #endif
    }
}
