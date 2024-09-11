using System;
using System.Collections.Generic;
using System.IO;
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


            var getExpressions = new Dictionary<string, Tuple<BindingGetExpression, GameObject>>();
            var setExpressions = new Dictionary<string, Tuple<BindingSetExpression, GameObject>>();

            foreach (var listener in listeners)
                listener.QueryExpressions(getExpressions, setExpressions);

            StringBuilder methods = new StringBuilder();
            HashSet<string> usings = new();

            usings.Add("System.Collections.Generic");
            usings.Add("Bodardr.Databinding.Runtime");
            usings.Add("UnityEngine");

            List<Tuple<string, string>> getters = new List<Tuple<string, string>>(getExpressions.Count);
            List<Tuple<string, string>> setters = new List<Tuple<string, string>>(setExpressions.Count);

            AOTCompileMethods(getExpressions, methods, getters, setters, usings);
            AOTCompileMethods(setExpressions, methods, getters, setters, usings);

            StringBuilder final = new StringBuilder();

            final.AppendLine("#if !UNITY_EDITOR");
            foreach (var use in usings)
                final.AppendLine($"using {use};");

            final.AppendLine("namespace Bodardr.Databinding.Compiled");
            final.AppendLine("{");
            final.AppendLine("\tpublic static class Bindings");
            final.AppendLine("\t{");

            final.AppendLine("\t\t[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
            final.AppendLine("\t\tprivate static void Initialize()");
            final.AppendLine("\t\t{");

            final.AppendLine(
                $"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.GetExpressions)} = new({getters.Count});");
            final.AppendLine(
                $"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.SetExpressions)} = new({setters.Count});");
            final.AppendLine();

            foreach (var (path, methodName) in getters)
                final.AppendLine(
                    $"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.GetExpressions)}.Add(\"{path}\",{methodName});");

            foreach (var (path, methodName) in setters)
                final.AppendLine(
                    $"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.SetExpressions)}.Add(\"{path}\",{methodName});");

            final.AppendLine("\t\t}");

            final.Append(methods);
            final.AppendLine("\t}");
            final.AppendLine("}");
            final.AppendLine("#endif");

            streamWriter.Write(final.ToString());

            //Reopen closed scenes
            for (int i = 0; i < openedScenePaths.Length; i++)
                EditorSceneManager.OpenScene(openedScenePaths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        }
        private static void AOTCompileMethods<T>(Dictionary<string, Tuple<T, GameObject>> getExpressions,
            StringBuilder methods, List<Tuple<string, string>> getters, List<Tuple<string, string>> setters,
            HashSet<string> usings) where T : IBindingExpression
        {
            foreach (var (expr, _) in getExpressions.Values)
            {
                methods.Append(expr.PreCompile(out var usingDirectives, getters, setters));

                foreach (var use in usingDirectives)
                    if (!string.IsNullOrEmpty(use))
                        usings.Add(use);
            }
        }
    }
}
