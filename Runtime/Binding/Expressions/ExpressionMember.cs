using System;
using System.Reflection;
namespace Bodardr.Databinding.Runtime
{
    struct ExpressionMember
    {
        public readonly MemberInfo MemberInfo;
        public readonly Type Type;

        public ExpressionMember(MemberInfo memberInfo, Type type)
        {
            MemberInfo = memberInfo;
            Type = type;
        }
    }
}
