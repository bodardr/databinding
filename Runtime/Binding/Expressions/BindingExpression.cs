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

        public D Expression { get; protected set; }

        public string[] AssemblyQualifiedTypeNames => assemblyQualifiedTypeNames;

        public bool ExpressionAlreadyCompiled => CompiledExpressions != null && CompiledExpressions.ContainsKey(Path);

        protected abstract Dictionary<string, D> CompiledExpressions { get; }

        public string Path
        {
            get => path;
            set => path = value;
        }

        public abstract void Compile(GameObject compilationContext);

        public void ResolveExpression(GameObject context)
        {
            if (Expression != null)
                return;

            if (ExpressionAlreadyCompiled)
                Expression = CompiledExpressions[Path];
            else
                Compile(context);
        }

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
        public void Compile(GameObject compilationContext);
    }
}