using System;
using UnityEngine;
namespace Bodardr.Databinding.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfEnumAttribute : PropertyAttribute
    {

        public ShowIfEnumAttribute(string memberName, int enumValue, bool invert = false)
        {
            MemberName = memberName;
            EnumValue = enumValue;
            Invert = invert;
        }
        public string MemberName { get; }
        public int EnumValue { get; }
        public bool Invert { get; }
    }
}
