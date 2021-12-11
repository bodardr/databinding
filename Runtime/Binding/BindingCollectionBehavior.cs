using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Bodardr.ObjectPooling;
using Bodardr.UI.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bodardr.Databinding.Runtime
{
    public class BindingCollectionBehavior : MonoBehaviour, ICollectionCallback
    {
        private IEnumerable<object> collection;

        private List<PoolableComponent<BindingBehavior>> pooledBindingBehaviors = new();
        private List<BindingBehavior> bindingBehaviors = new();

        [SerializeField]
        private bool setAmount = false;

        [ShowIf(nameof(setAmount))]
        [SerializeField]
        private int amount;

        [SerializeField]
        private bool useObjectPooling;

        [ShowIf(nameof(useObjectPooling), true)]
        [SerializeField]
        private GameObject prefab;

        [ShowIf(nameof(useObjectPooling))]
        [SerializeField]
        private PrefabPool pool;

        [Header("Children placement")]
        [SerializeField]
        private ChildPlacement placement = ChildPlacement.None;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<int> onClick;

        public BindingBehavior this[int index] =>
            useObjectPooling ? pooledBindingBehaviors[index].Content : bindingBehaviors[index];

        public int Count => useObjectPooling ? pooledBindingBehaviors.Count : bindingBehaviors.Count;

        private void Awake()
        {
            if (!setAmount)
                return;

            for (int i = 0; i < amount; i++)
                GetNewObject();
        }

        private void GetNewObject()
        {
            if (useObjectPooling)
            {
                var bindingBehavior = pool.Get<BindingBehavior>();
                bindingBehavior.Content.transform.SetParent(transform);
                pooledBindingBehaviors.Add(bindingBehavior);
            }
            else
            {
                var bindingBehavior = Instantiate(prefab, transform).GetComponent<BindingBehavior>();
                bindingBehaviors.Add(bindingBehavior);
            }
        }

        public void SetCollection(IEnumerable<object> collection)
        {
            this.collection = collection;
            UpdateCollection();
        }

        public void UpdateCollection()
        {
            if (collection == null)
                return;

            using var enumerator = collection.GetEnumerator();

            int i = 0;

            while (enumerator.MoveNext())
            {
                if (i >= Count)
                    GetNewObject();

                var bindingBehavior = this[i];

                bindingBehavior.gameObject.SetActive(true);

                var bindingTr = bindingBehavior.transform;

                if (placement == ChildPlacement.None)
                {
                    bindingTr.SetSiblingIndex(i);
                }
                else
                {
                    bindingTr.SetParent(transform.GetChild(i));
                    bindingTr.localPosition = Vector3.zero;
                }

                bindingBehavior.SetValueManual(enumerator.Current);

                i++;
            }

            for (var j = i; j < Count; j++)
                this[i].gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (!useObjectPooling)
                return;

            foreach (var pooledBehavior in pooledBindingBehaviors)
                pooledBehavior.Release();
        }

        public void OnClicked(int index)
        {
            onClick.Invoke(index);
        }
    }
}

public enum ChildPlacement
{
    None,
    OffsettedChildren
}