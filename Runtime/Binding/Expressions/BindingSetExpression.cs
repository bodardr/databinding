using System;
using System.Linq.Expressions;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    public delegate void SetDelegate(Component component, object newValue);

    [Serializable]
    public class BindingSetExpression : BindingExpression<SetDelegate>
    {
        public override bool ExpressionExists => BindableExpressionCompiler.setterExpresions.ContainsKey(Path);

        public override bool ResolveExpression()
        {
            if (!ExpressionExists)
            {
                Debug.LogWarning("Expression wasn't compiled before resolve. It will be compiled at this moment, it may take some time.");
                Compile();
                
                return false;
            }

            expression = BindableExpressionCompiler.setterExpresions[Path];
            return expression != null;
        }

        /// <param name="typeParams">
        /// <c>First argument </c>: Component type.
        /// <c>Second argument </c>: Setter Type</param>
        public override void Compile()
        {
            if (ExpressionExists)
            {
                expression = BindableExpressionCompiler.setterExpresions[Path];
                return;
            }

            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(AssemblyQualifiedTypeNames[0]) ||
                string.IsNullOrEmpty(AssemblyQualifiedTypeNames[1]))
                return;

            var componentType = Type.GetType(AssemblyQualifiedTypeNames[0]);
            var setterType = Type.GetType(AssemblyQualifiedTypeNames[1]);

            var setPathSplit = Path.Split('.');

            var valueParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");

            Expression rightExpr;
            if (setterType == typeof(string))
            {
                rightExpr = System.Linq.Expressions.Expression.Condition(
                    System.Linq.Expressions.Expression.Equal(valueParam,
                        System.Linq.Expressions.Expression.Constant(null)),
                    System.Linq.Expressions.Expression.Constant(string.Empty, typeof(string)), 
                    System.Linq.Expressions.Expression.Call(valueParam, "ToString", Type.EmptyTypes));
                
                
            }
            else if (setterType.IsValueType)
                rightExpr = System.Linq.Expressions.Expression.Unbox(valueParam, setterType);
            else
                rightExpr = System.Linq.Expressions.Expression.TypeAs(valueParam, setterType);


            var componentParam = System.Linq.Expressions.Expression.Parameter(typeof(Component), "component");

            Expression expr = System.Linq.Expressions.Expression.TypeAs(componentParam, componentType);
            for (var i = 1; i < setPathSplit.Length; i++)
                expr = System.Linq.Expressions.Expression.PropertyOrField(expr, setPathSplit[i]);

            expr = System.Linq.Expressions.Expression.Assign(expr, rightExpr);

            var compiledExpr = System.Linq.Expressions.Expression.Lambda<SetDelegate>(expr, componentParam, valueParam);

            if (compiledExpr.CanReduce)
                compiledExpr.Reduce();

            expression = compiledExpr.Compile();
            BindableExpressionCompiler.list.Add(this);
            BindableExpressionCompiler.setterExpresions.Add(Path, Expression);
        }
    }
}