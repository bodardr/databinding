using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bodardr.Databinding.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace Bodardr.Databinding.Runtime
{
    public class BindableExpressionsAOTCompiler : IPreprocessBuildWithReport
    {
        private const string CompiledExpressionsFolder = "Assets/Scripts/GENERATED BINDINGS";

        public int callbackOrder => 0;

        [MenuItem("Bindings/AOT-Compile")]
        public static void OnPreProcessBuildStatic()
        {
            BindableExpressionsAOTCompiler expressionCompiler = new();
            expressionCompiler.OnPreprocessBuild(null);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabaseUtility.CreateFolderIfNotExists(CompiledExpressionsFolder);

            var openedScenePaths = new string[SceneManager.sceneCount];
            for (var i = 0; i < openedScenePaths.Length; i++)
                openedScenePaths[i] = SceneManager.GetSceneAt(i).path;

            using var streamWriter = new StreamWriter(CompiledExpressionsFolder + "/Bindings.cs");

            var allExpressions = new Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>>();
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[i].path, OpenSceneMode.Single);
                foreach (var listener in Resources.FindObjectsOfTypeAll<BindingListenerBase>())
                    listener.QueryExpressions(allExpressions, true);
            }

            foreach (var listener in Resources.LoadAll<GameObject>("")
                .SelectMany(x => x.GetComponentsInChildren<BindingListenerBase>(true)))
                listener.QueryExpressions(allExpressions, true);

            var allAOTMethods = new StringBuilder();
            HashSet<string> usings = new();
            var final = new StringBuilder();
            var initializeStr = new StringBuilder();

            CompileAOTFor<BindingGetExpression>(allExpressions, allAOTMethods, usings, initializeStr);
            CompileAOTFor<BindingSetExpression>(allExpressions, allAOTMethods, usings, initializeStr);

            final.AppendLine("#if !UNITY_EDITOR");
            foreach (var use in usings)
                final.AppendLine($"using {use};");
            final.AppendLine();

            final.AppendLine("namespace Bodardr.Databinding.Compiled");
            final.AppendLine("{");
            final.AppendLine("\tpublic static class Bindings");
            final.AppendLine("\t{");

            final.AppendLine("\t\t[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            final.AppendLine("\t\tprivate static void Initialize()");
            final.AppendLine("\t\t{");

            final.AppendLine(initializeStr.ToString());
            
            final.AppendLine("\t\t}");

            final.Append(allAOTMethods);

            final.AppendLine("\t}");
            final.AppendLine("}");
            final.AppendLine("#endif");

            streamWriter.Write(final.ToString());

            //Reopen closed scenes
            for (var i = 0; i < openedScenePaths.Length; i++)
                EditorSceneManager.OpenScene(openedScenePaths[i],
                    i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        }
        private static void CompileAOTFor<T>(
            Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>> allExpressions,
            StringBuilder allAOTMethods, HashSet<string> usings, StringBuilder initializeStr)
        {
            var type = typeof(T);

            var expressions = $"{type.Name}.{nameof(BindingGetExpression.Expressions)}";
            var entries = new List<Tuple<string, string>>();

            var allExpressionsEntry = allExpressions.GetValueOrDefault(type);

            if (allExpressionsEntry == null)
                return;

            var expressionsOfType = allExpressionsEntry.ToArray();
            initializeStr.AppendLine($"\t\t\t{expressions}.Clear();");
            initializeStr.AppendLine($"\t\t\t{expressions}.EnsureCapacity({expressionsOfType.Length});");
            foreach (var (_, (expr, _)) in expressionsOfType)
            {
                allAOTMethods.Append(expr.AOTCompile(out var usingDirectives, entries));

                foreach (var use in usingDirectives)
                    if (!string.IsNullOrEmpty(use))
                        usings.Add(use);
            }

            foreach (var (path, methodName) in entries)
                initializeStr.AppendLine(
                    $"\t\t\t{expressions}.Add(\"{path}\",{methodName});");
        }
    }
}
