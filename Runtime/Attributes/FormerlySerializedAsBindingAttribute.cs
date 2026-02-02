using System;
namespace Bodardr.Databinding.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class FormerlySerializedAsBindingAttribute : Attribute
    {
        private string oldName;

        /// <summary>
        ///   <para></para>
        /// </summary>
        /// <param name="oldName">The name of the field before renaming.</param>
        public FormerlySerializedAsBindingAttribute(string oldName) => this.oldName = oldName;

        /// <summary>
        ///   <para>The name of the field before the rename.</para>
        /// </summary>
        public string OldName => oldName;
    }
}
