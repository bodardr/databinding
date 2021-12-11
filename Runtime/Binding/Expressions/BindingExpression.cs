using System;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    [Serializable]
    public abstract class BindingExpression<D> where D : Delegate
    {
        [SerializeField]
        protected string path;

        [SerializeField]
        protected string[] assemblyQualifiedTypeNames = new string[2];

        [NonSerialized]
        protected D expression;

        public string Path
        {
            get => path;
            set => path = value;
        }

        public D Expression => expression;

        public string[] AssemblyQualifiedTypeNames => assemblyQualifiedTypeNames;
        public abstract bool ExpressionExists { get; }

        public abstract bool ResolveExpression();

        public abstract void Compile();
    }
}