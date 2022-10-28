using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Bodardr.Utility.Runtime;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    public delegate object GetDelegate(object objectFrom);

    [Serializable]
    public class BindingGetExpression : BindingExpression<GetDelegate>
    {
        protected override Dictionary<string, GetDelegate> CompiledExpressions =>
            BindableExpressionCompiler.getterExpressions;

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
                    System.Linq.Expressions.Expression.Lambda<GetDelegate>(expr, parameterExpr);

                if (compiledExpr.CanReduce)
                    compiledExpr.Reduce();

                Expression = compiledExpr.Compile();
                BindableExpressionCompiler.getterExpressions.Add(Path, Expression);
            }
            catch (Exception e)
            {
                ThrowExpressionError(compilationContext, e);
            }
        }
    }
}