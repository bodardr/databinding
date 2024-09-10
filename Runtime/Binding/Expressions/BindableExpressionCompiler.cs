#define BDR_DATABINDING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Bodardr.Databinding.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bodardr.Databinding.Runtime
{
    public static class BindableExpressionCompiler
    {
        public static Dictionary<string, Func<object, object>> GetExpressions;
        public static Dictionary<string, Action<Component, object>> SetExpressions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            #if UNITY_EDITOR
            GetExpressions = new();
            SetExpressions = new();

            SceneManager.sceneLoaded += CompileAllExpressionsInScene;
            Application.quitting += UnSubscribe;

            for (int i = 0; i < SceneManager.sceneCount; i++)
                CompileAllExpressionsInScene(SceneManager.GetSceneAt(i));
            #endif

            BindingNode.InitializeStaticMembers();
        }

        #if UNITY_EDITOR
        private static void CompileAllExpressionsInScene(Scene scene, [Optional] LoadSceneMode loadSceneMode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var listeners = ComponentUtility.FindComponentsInScene<BindingListenerBase>(scene);

            var getExpressions = new Dictionary<string, Tuple<BindingGetExpression, GameObject>>();
            var setExpressions = new Dictionary<string, Tuple<BindingSetExpression, GameObject>>();

            foreach (var listener in listeners)
                listener.QueryExpressions(getExpressions, setExpressions);

            getExpressions.AsParallel().ForAll(x => x.Value.Item1.Compile(x.Value.Item2));
            setExpressions.AsParallel().ForAll(x => x.Value.Item1.Compile(x.Value.Item2));

            var bindingNodes = ComponentUtility.FindComponentsInScene<BindingNode>(scene);
            foreach (var node in bindingNodes)
                node.InitializeStaticTypeListeners();

            stopwatch.Stop();
            Debug.Log(
                $"<b>Databinding :</b> <b>{getExpressions.Count + setExpressions.Count}</b> Expressions compiled for <b>{scene.name}</b> in <b>{stopwatch.ElapsedMilliseconds}ms</b>");
        }

        private static void UnSubscribe()
        {
            SceneManager.sceneLoaded -= CompileAllExpressionsInScene;
            Application.quitting -= UnSubscribe;
        }
        #endif
    }
}
