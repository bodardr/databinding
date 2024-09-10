using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{

    [Serializable]
    public class BindingSetExpression : BindingExpression<Action<Component, object>>
    {
        protected override Dictionary<string, Action<Component, object>> CompiledExpressions =>
            BindableExpressionCompiler.SetExpressions;

        #if UNITY_EDITOR
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
                    System.Linq.Expressions.Expression.Lambda<Action<Component, object>>(expr, componentParam, valueParam);

                if (compiledExpr.CanReduce)
                    compiledExpr.Reduce();

                BindableExpressionCompiler.SetExpressions.Add(Path, compiledExpr.Compile());
            }
            catch(Exception e)
            {
                ThrowExpressionError(compilationContext, e);
            }
        }

        public override string PreCompile(out HashSet<string> usings, List<Tuple<string, string>> getters, List<Tuple<string, string>> setters)
        {
            usings = new HashSet<string>();

            var method = new StringBuilder();
            var propStr = new StringBuilder();

            var componentType = Type.GetType(AssemblyQualifiedTypeNames[0]);
            var valueType = Type.GetType(AssemblyQualifiedTypeNames[^1]);

            usings.Add(componentType.Namespace);
            usings.Add(valueType.Namespace);

            var properties = path.Split('.');

            var type = componentType;

            for (var i = 1; i < properties.Length; i++)
            {
                var member = properties[i];
                var memberInfo = type!.GetMember(member)[0];

                if (memberInfo.MemberType == MemberTypes.Property)
                    type = ((PropertyInfo)memberInfo).PropertyType;
                else
                    type = ((FieldInfo)memberInfo).FieldType;
                usings.Add(type.Namespace);

                propStr.AppendLine($".{member}");
            }

            var hashCode = GetHashCode();
            var methodName = $"Setter_{(hashCode < 0 ? "M" : "")}{Mathf.Abs(hashCode)}";
            method.AppendLine($"\t\tprivate static void {methodName}(Component component, object value)");
            method.AppendLine($"\t\t{{\n\t\t\t(({componentType.FullName})component){propStr} = ({valueType.FullName})value;\n\t\t}}");

            setters.Add(new(path, methodName));
            return method.ToString();
        }
        #endif
    }
}
