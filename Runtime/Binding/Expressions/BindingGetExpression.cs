using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    [Serializable]
    public class BindingGetExpression : BindingExpressionWithLocation<Func<object, object>>
    {
        #if UNITY_EDITOR
        public override string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries)
        {
            usings = new HashSet<string>();

            var method = new StringBuilder();
            var propStr = new StringBuilder();

            var properties = path.Split('.');

            var inputType = Type.GetType(AssemblyQualifiedTypeNames[0]);
            var type = inputType;

            usings.Add(type.Namespace);

            for (var i = 1; i < properties.Length; i++)
            {
                var member = properties[i];

                var memberInfo = type.GetMember(member)[0];

                type = memberInfo.MemberType switch
                {
                    MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                    _ => ((FieldInfo)memberInfo).FieldType
                };

                propStr.Append($".{member}");

                if (type.IsClass && i < properties.Length - 1)
                    propStr.Append('?');

                usings.Add(type.Namespace);
            }

            var hashCode = GetHashCode();
            var methodName = $"Getter_{(hashCode < 0 ? "M" : "")}{Mathf.Abs(hashCode)}";
            method.AppendLine($"\t\tprivate static object {methodName}(object binding)");
            method.AppendLine(
                $"\t\t{{\n\t\t\treturn (({inputType.FullName})binding){(inputType.IsClass ? "?" : "")}{propStr};\n\t\t}}");

            entries.Add(new(path, methodName));
            return method.ToString();
        }
        #endif

        public override void JITCompile(GameObject context)
        {
            try
            {
                if (string.IsNullOrEmpty(Path))
                {
                    ThrowExpressionError(context, new SerializationException($"{nameof(Path)} is empty"));
                    return;
                }

                foreach (var s in AssemblyQualifiedTypeNames)
                {
                    if (!string.IsNullOrEmpty(s))
                        continue;

                    ThrowExpressionError(context,
                        new SerializationException(
                            $"{nameof(AssemblyQualifiedTypeNames)} has an empty Type name, double check serialization."));
                    return;
                }

                const string bindingInput = "bindingInput";
                const string returnLabelStr = "returnLabel";

                var inputType = Type.GetType(AssemblyQualifiedTypeNames[0]);
                var properties = Path.Split('.');

                //Prepares member infos
                var parentMemberType = inputType;

                List<Expression> expressionBlock = new List<Expression>();
                List<ExpressionMember> memberInfos = GetMemberInfo(properties, parentMemberType);

                //Return label
                var returnLabel = Expression.Label(typeof(object), returnLabelStr);

                //Object Parameter
                var parameterExpr = Expression.Parameter(typeof(object));

                //Cast it to the input type
                var convertedParam = Expression.Convert(parameterExpr, inputType);

                //Declare a variable holding the cast value
                var varExpr = Expression.Variable(inputType, bindingInput);
                var assignedVarExpr = Expression.Assign(varExpr, convertedParam);

                expressionBlock.Add(assignedVarExpr);

                var handleNullExpr = HandleNullableTypes(varExpr, memberInfos, returnLabel);
                if (handleNullExpr != null)
                    expressionBlock.Add(handleNullExpr);

                Expression expr = varExpr;
                foreach (var memberInfo in memberInfos)
                    expr = AccessFieldOrProperty(expr, memberInfo.Type, memberInfo.MemberInfo);

                //Value to return, boxed as an object.
                expr = Expression.Convert(expr, typeof(object));
                expr = Expression.Return(returnLabel, expr, typeof(object));
                expressionBlock.Add(expr);

                //The default return label is added at the end.
                expressionBlock.Add(Expression.Label(returnLabel, Expression.Constant(null, typeof(object))));

                var finalExpression = Expression.Block(new[] { varExpr }, expressionBlock);
                var lambdaExpression = Expression.Lambda<Func<object, object>>(finalExpression, parameterExpr);

                if (lambdaExpression.CanReduce)
                    lambdaExpression.Reduce();

                ResolvedExpression = lambdaExpression.Compile();
                Expressions.Add(new ExpressionEntry<Func<object, object>>(Path, ResolvedExpression));
            }
            catch(Exception e)
            {
                ThrowExpressionError(context, e);
            }
        }

        private Expression HandleNullableTypes(ParameterExpression varExpr, List<ExpressionMember> memberInfos,
            LabelTarget returnLabel)
        {
            List<Expression> memberAccessExpressions = new List<Expression>();
            var nullExpr = Expression.Constant(null, typeof(object));

            //Check input parameter
            if (location is BindingExpressionLocation.InBindingNode or BindingExpressionLocation.InGameObject)
                memberAccessExpressions.Add(Expression.Equal(varExpr, nullExpr));

            //For each member
            for (int i = 0; i < memberInfos.Count - 1; i++)
            {
                if (!memberInfos[i].Type.IsClass)
                    continue;

                //Making repeated member access
                Expression expr = varExpr;
                for (int j = 0; j <= i; j++)
                {
                    var memberInfo = memberInfos[j];
                    expr = AccessFieldOrProperty(expr, memberInfo.Type, memberInfo.MemberInfo);
                }

                expr = Expression.Equal(expr, nullExpr);
                memberAccessExpressions.Add(expr);
            }

            if (memberAccessExpressions.Count < 1)
                return varExpr;

            Expression ifNullExpr = memberAccessExpressions[0];
            for (int i = 1; i < memberAccessExpressions.Count; i++)
                ifNullExpr = Expression.OrElse(ifNullExpr, memberAccessExpressions[i]);

            ifNullExpr = Expression.IfThen(ifNullExpr, Expression.Return(returnLabel, nullExpr, typeof(object)));
            return ifNullExpr;
        }

        private static List<ExpressionMember> GetMemberInfo(string[] properties, Type parentMemberType)
        {
            var memberInfos = new List<ExpressionMember>();
            for (int i = 1; i < properties.Length; i++)
            {
                var memberInfo = parentMemberType?.GetMember(properties[i])[0];
                var memberType = memberInfo!.MemberType switch
                {
                    MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                    _ => ((FieldInfo)memberInfo).FieldType
                };

                memberInfos.Add(new ExpressionMember(memberInfo, memberType));
                parentMemberType = memberType;
            }
            return memberInfos;
        }

        private static Expression AccessFieldOrProperty(Expression expr, Type type, MemberInfo memberInfo)
        {
            //If this member comes from a static type
            var isStatic = memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.IsStatic,
                PropertyInfo propertyInfo => propertyInfo.GetAccessors(true)[0].IsStatic,
                _ => false
            };

            isStatic = isStatic || type.IsSealed && type.IsAbstract;

            if (isStatic)
                expr = Expression.MakeMemberAccess(null, memberInfo);
            else
                expr = Expression.PropertyOrField(expr, memberInfo.Name);
            return expr;
        }

        public void Subscribe(BindingListenerBase listener, BindingNode node)
        {
            switch (location)
            {
                case BindingExpressionLocation.InBindingNode:
                    node.AddListener(listener, Path);
                    break;
            }
        }

        public void Unsubscribe(BindingListenerBase listener, BindingNode node)
        {
            switch (location)
            {
                case BindingExpressionLocation.InBindingNode:
                    node.RemoveListener(listener, Path);
                    break;
            }
        }

        public object Invoke(object source, GameObject context)
        {
            try
            {
                switch (location)
                {
                    case BindingExpressionLocation.InGameObject:
                        return ResolvedExpression(component);
                    case BindingExpressionLocation.InBindingNode:
                        return ResolvedExpression(source);
                    case BindingExpressionLocation.Static:
                    default:
                        return ResolvedExpression(null);
                }
            }
            catch(Exception)
            {
                Debug.LogError($"<b><color=red>Error with Get Expression {Path} in {context.name}</color></b>",
                    context);
                throw;
            }
        }
    }
}
