using System;
namespace Bodardr.Databinding.Runtime.Extensions
{
    public class ToStringExtension : IBindingExtension
    {

        public Type OutputType => typeof(string);
        public object Process(object obj) => obj.ToString();
    }
}
