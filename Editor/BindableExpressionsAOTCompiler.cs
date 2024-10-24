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

        public static Dictionary<string, Func<object, object>> strDic = new();

        [MenuItem("Bindings/AOT-Compile")]
        public static void OnPreProcessBuildStatic()
        {
            BindableExpressionsAOTCompiler expressionCompiler = new();
            expressionCompiler.OnPreprocessBuild(null);
        }

        private static void WriteAsmdef()
        {

            var asmdefPath = CompiledExpressionsFolder + "/com.bodardr.databinding.compiled.asmdef";
            if (!File.Exists(asmdefPath))
            {
                using var streamWriter = new StreamWriter(asmdefPath);
                streamWriter.Write(@"{
                                            ""name"": ""com.bodardr.databinding.compiled"",
                                                ""rootNamespace"": ""Bodardr.Databinding.Compiled"",
                                                ""references"": [],
                                                ""includePlatforms"": [],
                                                ""excludePlatforms"": [],
                                                ""allowUnsafeCode"": false,
                                                ""overrideReferences"": false,
                                                ""precompiledReferences"": [],
                                                ""autoReferenced"": true,
                                                ""defineConstraints"": [],
                                                ""versionDefines"": [],
                                                ""noEngineReferences"": false
                                        }");
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabaseUtility.CreateFolderIfNotExists(CompiledExpressionsFolder);

            string[] openedScenePaths = new string[SceneManager.sceneCount];
            for (int i = 0; i < openedScenePaths.Length; i++)
                openedScenePaths[i] = SceneManager.GetSceneAt(i).path;

            using StreamWriter streamWriter = new StreamWriter(CompiledExpressionsFolder + "/Bindings.cs");

            HashSet<BindingListenerBase> listeners = new();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                EditorSceneManager.OpenScene(EditorBuildSettings.scenes[i].path, OpenSceneMode.Single);
                foreach (var listener in Resources.FindObjectsOfTypeAll<BindingListenerBase>())
                    listeners.Add(listener);
            }

            var allExpressions = new Dictionary<string, Tuple<IBindingExpression, GameObject>>();

            foreach (var listener in listeners)
                listener.QueryExpressions(allExpressions);

            StringBuilder allAOTMethods = new StringBuilder();
            HashSet<string> usings = new();

            usings.Add("Bodardr.Databinding.Runtime");
            usings.Add("UnityEngine");

            StringBuilder final = new StringBuilder();

            final.AppendLine("#if !UNITY_EDITOR");
            foreach (var use in usings)
                final.AppendLine($"using {use};");

            final.AppendLine("namespace Bodardr.Databinding.Compiled");
            final.AppendLine("{");
            final.AppendLine("\tpublic static class Bindings");
            final.AppendLine("\t{");

            final.AppendLine("\t\t[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            final.AppendLine("\t\tprivate static void Initialize()");
            final.AppendLine("\t\t{");

            CompileAOTFor<BindingGetExpression>(allExpressions, allAOTMethods, usings, final);
            CompileAOTFor<BindingSetExpression>(allExpressions, allAOTMethods, usings, final);
            
            final.AppendLine("\t\t}");
            
            final.Append(allAOTMethods);

            final.AppendLine("\t}");
            final.AppendLine("}");
            final.AppendLine("#endif");

            streamWriter.Write(final.ToString());

            //Reopen closed scenes
            for (int i = 0; i < openedScenePaths.Length; i++)
                EditorSceneManager.OpenScene(openedScenePaths[i],
                    i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        }
        private static void CompileAOTFor<T>(Dictionary<string, Tuple<IBindingExpression, GameObject>> allExpressions,
            StringBuilder allAOTMethods, HashSet<string> usings, StringBuilder final)
        {
            var type = typeof(T);

            var expressions = $"{type.Name}.{nameof(BindingGetExpression.Expressions)}";
            var entries = new List<Tuple<string, string>>();

            var expressionsOfType = allExpressions.Values.Where(x => x.Item1 is T).ToArray();
            final.AppendLine($"\t\t\t{expressions}.EnsureCapacity({expressionsOfType.Length});");
            foreach (var (expr, _) in expressionsOfType)
            {
                allAOTMethods.Append(expr.AOTCompile(out var usingDirectives, entries));

                foreach (var use in usingDirectives)
                    if (!string.IsNullOrEmpty(use))
                        usings.Add(use);
            }

            foreach (var (path, methodName) in entries)
                final.AppendLine(
                    $"\t\t\t{expressions}.Add(new(\"{path}\",{methodName}));");
        }
    }
}
