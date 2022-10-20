﻿#define BDR_DATABINDING

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Dictionary<string, GetDelegate> getterExpresions;
        public static Dictionary<string, SetDelegate> setterExpresions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void CompileOnSceneLoad()
        {
            SceneManager.sceneLoaded += CompileAllExpressionsInScene;
            Application.quitting += UnSubscribe;

            var activeScene = SceneManager.GetActiveScene();

            BindingBehavior.InitializeStaticMembers();
            if (activeScene.isLoaded)
                CompileAllExpressionsInScene(activeScene);
        }

        private static void CompileAllExpressionsInScene(Scene scene, [Optional] LoadSceneMode loadSceneMode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var listeners = ComponentUtility.FindComponentsInScene<BindingListenerBase>(scene);

            getterExpresions ??= new Dictionary<string, GetDelegate>(listeners.Count);
            setterExpresions ??= new Dictionary<string, SetDelegate>(listeners.Count);

            var i = 0;
            try
            {
                for (; i < listeners.Count; i++)
                    listeners[i].InitializeAndCompile();
            }
            catch (Exception e)
            {
                Debug.LogError($"<b>Databinding</b> : Error in {listeners[i].name} : {e}", listeners[i].gameObject);
            }

            var bindingBehaviors = Resources.FindObjectsOfTypeAll<BindingBehavior>();

            foreach (var bindingBehavior in bindingBehaviors)
                bindingBehavior.InitializeStaticListeners();

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