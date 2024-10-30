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
                if (!instance)
                    CreateInstance();

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CreateInstance()
        {
            GameObject go = new GameObject(nameof(UnityDispatcher), typeof(UnityDispatcher));
            instance = go.GetComponent<UnityDispatcher>();
            instance.hideFlags = HideFlags.HideAndDontSave;
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
