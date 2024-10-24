namespace Bodardr.Databinding.Runtime
{
    public struct BindingExpressionErrorContext
    {
        public enum ErrorType
        {
            OK = default,
            EMPTY_EXPRESSION,
            HIERARCHY_TYPE_MISMATCH,
            NO_MATCHING_COMPONENT_IN_GAMEOBJECT,
            COULD_NOT_FIND_MEMBER,
            BINDING_NODE_TYPE_MISMATCH
        }

        public ErrorType Error { get; set; }
        public string Message { get; set; }

        public static BindingExpressionErrorContext OK => new BindingExpressionErrorContext(ErrorType.OK);

        public BindingExpressionErrorContext(ErrorType type)
        {
            Error = type;
            Message = string.Empty;
        }

        public BindingExpressionErrorContext(ErrorType type, string message)
        {
            Error = type;
            Message = message;
        }
    }
}
