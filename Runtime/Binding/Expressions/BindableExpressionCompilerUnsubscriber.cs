using Bodardr.UI.Runtime;

namespace Bodardr.Databinding.Runtime
{
    public class BindableExpressionCompilerUnsubscriber : DontDestroyOnLoad<BindableExpressionCompilerUnsubscriber>
    {
        private void OnApplicationQuit()
        {
            BindableExpressionCompiler.UnSubscribe();
        }
    }
}