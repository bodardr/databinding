using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Bodardr.Databinding.Runtime.Expressions
{
    public delegate object GetDelegate(object objectFrom);
    
    [Serializable]
    public class BindingGetExpression : BindingExpression<GetDelegate>
    {
        public override bool ExpressionExists => BindableExpressionCompiler.getterExpresions.ContainsKey(Path);

        public override bool ResolveExpression()
        {
            if (!ExpressionExists)
            {
                Debug.LogWarning("Expression wasn't compiled before resolve. It will be compiled at this moment, it may take some time.");
                Compile();
                
                return false;
            }
            
            expression = BindableExpressionCompiler.getterExpresions[Path];
            return expression != null;
        }

        public override void Compile()
        {
            if (ExpressionExists)
            {
                expression = BindableExpressionCompiler.getterExpresions[Path];
                return;
            }
            
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(AssemblyQualifiedTypeNames[0]))
                return;
            
            var type = Type.GetType(AssemblyQualifiedTypeNames[0]);
            var properties = path.Split('.');

            var memberType = type;
            MemberInfo[] memberInfos = new MemberInfo[properties.Length - 1];
            for (int i = 0; i < properties.Length - 1; i++)
            {
                memberInfos[i] = memberType?.GetMember(properties[i + 1])[0];

                if (memberInfos[i].MemberType == MemberTypes.Property)
                    memberType = ((PropertyInfo)memberInfos[i]).PropertyType;
                else
                    memberType = ((FieldInfo)memberInfos[i]).FieldType;
            }

            var param = System.Linq.Expressions.Expression.Parameter(typeof(object));
            var cast = System.Linq.Expressions.Expression.TypeAs(param, type);

            Expression expr = cast;

            for (var i = 1; i < properties.Length; i++)
            {
                bool isStatic = false;
                var memberInfo = memberInfos[i - 1];

                if (memberInfo.MemberType == MemberTypes.Property)
                    isStatic = ((PropertyInfo)memberInfo).GetAccessors(true)[0].IsStatic;
                else
                    isStatic = ((FieldInfo)memberInfo).IsStatic;
                
                if(isStatic)
                    expr = System.Linq.Expressions.Expression.MakeMemberAccess(null, memberInfo);
                else
                    expr = System.Linq.Expressions.Expression.PropertyOrField(expr, properties[i]);
            }

            expr = System.Linq.Expressions.Expression.TypeAs(expr, typeof(object));

            var compiledExpr = System.Linq.Expressions.Expression.Lambda<GetDelegate>(expr, param);

            if (compiledExpr.CanReduce)
                compiledExpr.Reduce();

            expression = compiledExpr.Compile();
            BindableExpressionCompiler.getterExpresions.Add(Path, Expression);
        }
    }
}