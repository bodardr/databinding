﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{

    [Serializable]
    public class BindingSetExpression : BindingExpressionWithLocation<Action<object, object>>
    {
        #if UNITY_EDITOR
        public override string AOTCompile(out HashSet<string> usings, List<Tuple<string, string>> entries)
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

                propStr.Append($".{member}");
                if (type.IsClass && i < properties.Length - 1)
                    propStr.Append('?');

                usings.Add(type.Namespace);
            }

            var hashCode = GetHashCode();
            var methodName = $"Setter_{(hashCode < 0 ? "M" : "")}{Mathf.Abs(hashCode)}";

            method.AppendLine($"\t\tprivate static void {methodName}(object input, object value)");

            if (valueType == typeof(string))
                method.AppendLine(
                    $"\t\t{{\n\t\t\t(({componentType.FullName})input){propStr} = value?.ToString();\n\t\t}}");
            else
                method.AppendLine(
                    $"\t\t{{\n\t\t\t(({componentType.FullName})input){propStr} = ({valueType.FullName})value;\n\t\t}}");

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

                var componentType = Type.GetType(AssemblyQualifiedTypeNames[0]);
                var setterType = Type.GetType(AssemblyQualifiedTypeNames[^1]);

                var setPathSplit = Path.Split('.');

                var valueParam = Expression.Parameter(typeof(object), "value");

                Expression rightExpr;
                if (setterType == typeof(string))
                {
                    rightExpr = Expression.Condition(
                        Expression.Equal(valueParam,
                            Expression.Constant(null)),
                        Expression.Constant(string.Empty, typeof(string)),
                        Expression.Call(valueParam, "ToString", Type.EmptyTypes));
                }
                else if (setterType.IsValueType)
                    rightExpr = Expression.Unbox(valueParam, setterType);
                else
                    rightExpr = Expression.TypeAs(valueParam, setterType);

                var componentParam = Expression.Parameter(typeof(object), "component");

                Expression memberSetExpr = Expression.TypeAs(componentParam, componentType);
                for (var i = 1; i < setPathSplit.Length; i++)
                    memberSetExpr = Expression.PropertyOrField(memberSetExpr, setPathSplit[i]);

                memberSetExpr = Expression.Assign(memberSetExpr, rightExpr);

                var lambdaExpression =
                    Expression.Lambda<Action<object, object>>(memberSetExpr, componentParam, valueParam);

                if (lambdaExpression.CanReduce)
                    lambdaExpression.Reduce();

                ResolvedExpression = lambdaExpression.Compile();
                Expressions.Add(new ExpressionEntry<Action<object, object>>(Path, ResolvedExpression));
            }
            catch(Exception e)
            {
                ThrowExpressionError(context, e);
            }
        }

        public void Invoke(object source, object destination, GameObject context)
        {
            try
            {

                switch (location)
                {
                    case BindingExpressionLocation.InGameObject:
                        ResolvedExpression(component, destination);
                        break;
                    case BindingExpressionLocation.InBindingNode:
                        ResolvedExpression(source, destination);
                        break;
                    case BindingExpressionLocation.Static:
                    default:
                        ResolvedExpression(null, destination);
                        break;
                }
            }
            catch(Exception)
            {
                Debug.LogError($"<b><color=red>Error with Set Expression {Path} in {context.name}</color></b>", context);
                throw;
            }
        }
    }
}
