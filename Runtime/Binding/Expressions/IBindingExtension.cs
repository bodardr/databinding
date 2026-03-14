using System;

namespace Bodardr.Databinding.Runtime
{
    public interface IBindingExtension
    {
        public Type OutputType { get; }
        
        public object Process(object obj);
    }
}
