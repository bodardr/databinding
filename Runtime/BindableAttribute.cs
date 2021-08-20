using System;

/// <summary>
///     Auto-implements properties with the INotifyChanged interface.
///     <remarks>
///         To specifically deny any auto-implementation, please use the
///         DoNotBind attribute.
///     </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BindableAttribute : Attribute
{
}