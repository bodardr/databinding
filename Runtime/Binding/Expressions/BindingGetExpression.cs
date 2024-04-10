using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Bodardr.Utility.Runtime;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{

    [Serializable]
    public class BindingGetExpression : BindingExpression<Func<object, object>>
    {
        protected override Dictionary<string, Func<object, object>> CompiledExpressions =>
            BindableExpressionCompiler.GetExpressions;

        #if UNITY_EDITOR
        public override void Compile(GameObject compilationContext)
        {
            try
            {
                //Returns if Path and input Type is invalid.
                if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(AssemblyQualifiedTypeNames[0]))
                    return;

                var inputType = Type.GetType(AssemblyQualifiedTypeNames[0]);
                var properties = path.Split('.');

                //Prepares member infos
                var parentMemberType = inputType;

                //memberInfos : <MemberInfo member, bool isValueType>
                Tuple<MemberInfo, Type>[] memberInfos = new Tuple<MemberInfo, Type>[properties.Length - 1];
                for (int i = 0; i < properties.Length - 1; i++)
                {
                    var memberInfo = parentMemberType?.GetMember(properties[i + 1])[0];
                    Type memberType;

                    if (memberInfo.MemberType == MemberTypes.Property)
                        memberType = ((PropertyInfo)memberInfo).PropertyType;
                    else
                        memberType = ((FieldInfo)memberInfo).FieldType;

                    memberInfos[i] = new Tuple<MemberInfo, Type>(memberInfo, memberType);
                    parentMemberType = memberType;
                }

                var parameterExpr = System.Linq.Expressions.Expression.Parameter(typeof(object));
                var inputParameterToDataTypeExpression = System.Linq.Expressions.Expression.Convert(
                    parameterExpr, inputType);

                Expression expr = inputParameterToDataTypeExpression;

                //Final member access.
                for (int i = 0; i < memberInfos.Length; i++)
                {
                    //If this member comes from a static type
                    if (memberInfos[i].Item1.ReflectedType.IsStaticType())
                        expr =
                            System.Linq.Expressions.Expression.MakeMemberAccess(null, memberInfos[i].Item1);
                    else
                        expr =
                            System.Linq.Expressions.Expression.PropertyOrField(expr, properties[i + 1]);
                }

                expr = System.Linq.Expressions.Expression.TypeAs(expr, typeof(object));

                var compiledExpr =
                    System.Linq.Expressions.Expression.Lambda<Func<object, object>>(expr, parameterExpr);

                if (compiledExpr.CanReduce)
                    compiledExpr.Reduce();

                BindableExpressionCompiler.GetExpressions.Add(Path, compiledExpr.Compile());
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

            var properties = path.Split('.');

            var inputType = Type.GetType(AssemblyQualifiedTypeNames[0]);
            var type = inputType;
            usings.Add(type.Namespace);

            for (var i = 0; i < properties.Length - 1; i++)
            {
                var member = properties[i + 1];
                propStr.AppendLine($".{member}");

                var memberInfo = type!.GetMember(member)[0];

                if (memberInfo.MemberType == MemberTypes.Property)
                    type = ((PropertyInfo)memberInfo).PropertyType;
                else
                    type = ((FieldInfo)memberInfo).FieldType;

                usings.Add(type.Namespace);
            }

            var hashCode = GetHashCode();
            var methodName = $"Getter_{(hashCode < 0 ? "M" : "")}{Mathf.Abs(hashCode)}";
            method.AppendLine($"\t\tprivate static object {methodName}(object binding)");
            method.AppendLine($"\t\t{{\n\t\t\treturn (({inputType.FullName})binding){propStr};\n\t\t}}");

            getters.Add(new(path, methodName));
            return method.ToString();
        }
        #endif
    }
}
