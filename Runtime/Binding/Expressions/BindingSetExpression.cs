using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    public delegate void SetDelegate(Component component, object newValue);

    [Serializable]
    public class BindingSetExpression : BindingExpression<SetDelegate>
    {
        protected override Dictionary<string, SetDelegate> CompiledExpressions =>
            BindableExpressionCompiler.setterExpressions;

        public override void Compile(GameObject compilationContext)
        {
            try
            {
                if (string.IsNullOrEmpty(Path) || AssemblyQualifiedTypeNames.Any(string.IsNullOrWhiteSpace))
                    return;

                var componentType = Type.GetType(AssemblyQualifiedTypeNames[0]);
                var setterType = Type.GetType(AssemblyQualifiedTypeNames[^1]);

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

                var compiledExpr =
                    System.Linq.Expressions.Expression.Lambda<SetDelegate>(expr, componentParam, valueParam);

                if (compiledExpr.CanReduce)
                    compiledExpr.Reduce();

                Expression = compiledExpr.Compile();
                BindableExpressionCompiler.setterExpressions.Add(Path, Expression);
            }
            catch (Exception e)
            {
                ThrowExpressionError(compilationContext, e);
            }
        }
    }
}