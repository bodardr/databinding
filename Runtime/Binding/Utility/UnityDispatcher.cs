using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    public class UnityDispatcher : MonoBehaviour
    {
        private static UnityDispatcher instance;
        private static readonly Queue<Action> actionQueue = new();

        public static UnityDispatcher Instance
        {
            get
            {
                if (instance == null)
                    CreateInstance();

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CreateInstance()
        {
            var go = new GameObject(nameof(UnityDispatcher), typeof(UnityDispatcher))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            instance = go.GetComponent<UnityDispatcher>();
            DontDestroyOnLoad(go);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            lock (actionQueue)
                while (actionQueue.Count > 0)
                    actionQueue.Dequeue()();
        }

        public static void EnqueueOnUnityThread(Action action)
        {
            if (!Instance)
                return;

            lock (actionQueue)
                actionQueue.Enqueue(action);
        }
    }
}
