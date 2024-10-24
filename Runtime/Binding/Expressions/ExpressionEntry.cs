using System;
namespace Bodardr.Databinding.Runtime
{
    public struct ExpressionEntry<TExpr> where TExpr : Delegate
    {
        public readonly string Path;
        public readonly TExpr Expression;

        public ExpressionEntry(string path)
        {
            Path = path;
            Expression = null;
        }

        public ExpressionEntry(string path, TExpr expression)
        {
            Path = path;
            Expression = expression;
        }
    }
}
