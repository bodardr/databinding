using System;
using System.Collections.Generic;
namespace Bodardr.Databinding.Runtime
{
    public class ExpressionEntryComparer<T> : IEqualityComparer<ExpressionEntry<T>> where T : Delegate
    {
        public bool Equals(ExpressionEntry<T> x, ExpressionEntry<T> y) => x.Path == y.Path;
        public int GetHashCode(ExpressionEntry<T> obj) => obj.Path.GetHashCode();
    }
}
