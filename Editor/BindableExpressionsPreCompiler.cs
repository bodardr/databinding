﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bodardr.Databinding.Runtime.Expressions;
using Bodardr.Utility.Runtime;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace Bodardr.Databinding.Runtime
{
    public class BindableExpressionsPreCompiler : IPreprocessBuildWithReport
    {
        private const string CompiledExpressionsFolder = "Assets/Scripts/GENERATED BINDINGS";

        public int callbackOrder => 0;

        public static Dictionary<string, Func<object, object>> strDic = new();

        [MenuItem("Bindings/Pre-Compile")]
        public static void OnPreProcessBuildStatic()
        {
            BindableExpressionsPreCompiler expressionCompiler = new();
            expressionCompiler.OnPreprocessBuild(null);
        }

        private static void WriteAsmdef()
        {

            var asmdefPath = CompiledExpressionsFolder + "/com.bodardr.databinding.compiled.asmdef";
            if (!File.Exists(asmdefPath))
            {
                using (var streamWriter = new StreamWriter(asmdefPath))
                {
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
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabaseUtility.CreateFolderIfNotExists(CompiledExpressionsFolder);

            using (StreamWriter streamWriter = new StreamWriter(CompiledExpressionsFolder + "/Bindings.cs"))
            {
                HashSet<BindingListenerBase> listeners = new();
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    listeners.AddRange(ComponentUtility.FindComponentsInScene<BindingListenerBase>(SceneManager.GetSceneByBuildIndex(i)));
                }
                
                listeners.AddRange(Resources.FindObjectsOfTypeAll<BindingListenerBase>());

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

                PreCompileMethods(getExpressions, methods, getters, setters, usings);
                PreCompileMethods(setExpressions, methods, getters, setters, usings);

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

                final.AppendLine($"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.GetExpressions)} = new({getters.Count});");
                final.AppendLine($"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.SetExpressions)} = new({setters.Count});");
                final.AppendLine();

                foreach (var (path, methodName) in getters)
                    final.AppendLine($"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.GetExpressions)}.Add(\"{path}\",{methodName});");

                foreach (var (path, methodName) in setters)
                    final.AppendLine($"\t\t\t{nameof(BindableExpressionCompiler)}.{nameof(BindableExpressionCompiler.SetExpressions)}.Add(\"{path}\",{methodName});");

                final.AppendLine("\t\t}");

                final.Append(methods);
                final.AppendLine("\t}");
                final.AppendLine("}");
                final.AppendLine("#endif");

                streamWriter.Write(final.ToString());
            }
        }
        private static void PreCompileMethods<T>(Dictionary<string, Tuple<T, GameObject>> getExpressions, StringBuilder methods, List<Tuple<string, string>> getters, List<Tuple<string, string>> setters, HashSet<string> usings) where T : IBindingExpression
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