#define BDR_DATABINDING

using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bodardr.Databinding.Runtime
{
    public class BindableExpressionCompiler
    {
        public static Dictionary<string, GetDelegate> getterExpresions;
        public static Dictionary<string, SetDelegate> setterExpresions;
        public static List<BindingSetExpression> list = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CompileOnSceneLoad()
        {
            SceneManager.sceneLoaded += CompileAllExpressions;

            //Initialize instance via property accessor.
            var instance = BindableExpressionCompilerUnsubscriber.Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CompileAllExpressions()
        {
            var timeBefore = Time.realtimeSinceStartup;
            Debug.Log($"Time before compile : {timeBefore}");

            var listeners = Resources.FindObjectsOfTypeAll<BindingListenerBase>();

            getterExpresions ??= new Dictionary<string, GetDelegate>(listeners.Length);
            setterExpresions ??= new Dictionary<string, SetDelegate>(listeners.Length);

            var i = 0;
            try
            {
                for (; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    listener.InitializeAndCompile();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in {listeners[i].name} : {e}", listeners[i].transform);
            }

            BindingBehavior.InitializeStaticMembers();
            var bindingBehaviors = Resources.FindObjectsOfTypeAll<BindingBehavior>();

            foreach (var bindingBehavior in bindingBehaviors)
                bindingBehavior.InitializeStaticListeners();

            var timeAfter = Time.realtimeSinceStartup;
            Debug.Log($"Time after compile : {timeAfter}. Diff : {timeAfter - timeBefore}");
        }

        public static void UnSubscribe() => SceneManager.sceneLoaded -= CompileAllExpressions;

        private static void CompileAllExpressions(Scene scene, LoadSceneMode loadSceneMode) => CompileAllExpressions();
    }
}