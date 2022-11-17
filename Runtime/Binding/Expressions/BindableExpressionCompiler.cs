#define BDR_DATABINDING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Bodardr.Databinding.Runtime.Expressions;
using Bodardr.Utility.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bodardr.Databinding.Runtime
{
    public static class BindableExpressionCompiler
    {
        public static Dictionary<string, GetDelegate> getterExpressions;
        public static Dictionary<string, SetDelegate> setterExpressions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void CompileOnSceneLoad()
        {
            SceneManager.sceneLoaded += CompileAllExpressionsInScene;
            Application.quitting += UnSubscribe;

            getterExpressions ??= new Dictionary<string, GetDelegate>();
            setterExpressions ??= new Dictionary<string, SetDelegate>();

            BindingBehavior.InitializeStaticMembers();
        }

        private static void CompileAllExpressionsInScene(Scene scene, [Optional] LoadSceneMode loadSceneMode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var listeners = ComponentUtility.FindComponentsInScene<BindingListenerBase>(scene);

            var expressions = new Dictionary<string, Tuple<IBindingExpression, GameObject>>();
            foreach (var listener in listeners)
                listener.QueryExpressions(expressions);

            expressions.AsParallel().ForAll(x => x.Value.Item1.Compile(x.Value.Item2));

            var bindingBehaviors = ComponentUtility.FindComponentsInScene<BindingBehavior>(scene);
            foreach (var bindingBehavior in bindingBehaviors)
                bindingBehavior.InitializeStaticTypeListeners();

            stopwatch.Stop();
            Debug.Log($"Binding expressions compiled for {scene.name} in <b>{stopwatch.ElapsedMilliseconds}ms</b>");
        }

        private static void UnSubscribe()
        {
            SceneManager.sceneLoaded -= CompileAllExpressionsInScene;
            Application.quitting -= UnSubscribe;
        }
    }
}