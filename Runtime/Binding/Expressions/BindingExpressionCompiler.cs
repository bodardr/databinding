﻿#define BDR_DATABINDING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bodardr.Databinding.Runtime
{
    public static class BindingExpressionCompiler
    {
#if !ENABLE_IL2CPP || UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += JITCompileAllExpressionsInScene;
            Application.quitting += UnSubscribe;

            for (int i = 0; i < SceneManager.sceneCount; i++)
                JITCompileAllExpressionsInScene(SceneManager.GetSceneAt(i));
        }

        private static void JITCompileAllExpressionsInScene(Scene scene, [Optional] LoadSceneMode loadSceneMode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var listeners = Resources.FindObjectsOfTypeAll<BindingListenerBase>();
            var expressions =
                new Dictionary<Type, Dictionary<string, Tuple<IBindingExpression, GameObject>>>(listeners.Length);

            foreach (var listener in listeners)
                listener.QueryExpressions(expressions);

            expressions.Values.AsParallel().SelectMany(x => x.Values).ForAll(x => x.Item1.JITCompile(x.Item2));

            var bindingNodes = ComponentUtility.FindComponentsInScene<BindingNode>(scene);
            foreach (var node in bindingNodes)
                if (node.BindingMethod == BindingMethod.Static)
                    node.InitializeStaticTypeListeners();

            stopwatch.Stop();
            Debug.Log(
                $"<b>Databinding :</b> <b>{expressions.Count}</b> Expressions compiled for <b>{scene.name}</b> in <b>{stopwatch.ElapsedMilliseconds}ms</b>");
        }

        private static void UnSubscribe()
        {
            SceneManager.sceneLoaded -= JITCompileAllExpressionsInScene;
            Application.quitting -= UnSubscribe;
        }
#endif
    }
}
