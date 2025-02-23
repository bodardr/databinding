﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Bodardr.Databinding.Runtime
{
    public class BindingCollectionBehavior : MonoBehaviour, ICollectionCallback, INotifyPropertyChanged
    {
        private readonly List<BindingNode> bindingNodes = new();
        private ObjectPool<BindingNode> objectPool;

        private IEnumerable collection;
        private bool initialized = false;

        [Header("Instantiation")]
        [SerializeField]
        private bool setAmount = false;

        [ShowIf(nameof(setAmount))]
        [SerializeField]
        private int amount;

        [SerializeField]
        private bool useObjectPooling;

        [SerializeField]
        private GameObject prefab;

        [Header("Children placement")]
        [SerializeField]
        private ChildPlacement placement = ChildPlacement.None;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<int> onClick;

        public BindingNode this[int index] => bindingNodes[index];

        public int Count => bindingNodes.Count;

        public IEnumerable Collection
        {
            get => collection;
            set
            {
                if (!initialized)
                    Awake();

                collection = value;
                UpdateCollection();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Collection)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Awake()
        {
            if (initialized)
                return;

            bindingNodes?.Clear();
            objectPool?.Clear();

            if (useObjectPooling)
            {
                objectPool = new ObjectPool<BindingNode>(
                    () => Instantiate(prefab, transform).GetComponent<BindingNode>(),
                    node => node.gameObject.SetActive(true),
                    node => node.gameObject.SetActive(false));
            }
            else
            {
                foreach (Transform child in transform)
                {
                    var bindingNode = child.GetComponent<BindingNode>();

                    if (bindingNode != null)
                        bindingNodes.Add(bindingNode);
                }
            }

            initialized = true;

            if (!setAmount)
                return;

            for (int i = 0; i < amount; i++)
                GetNewObject();
        }

        private void OnDestroy()
        {
            if (!useObjectPooling)
                return;

            objectPool?.Clear();
        }

        public void OnItemClicked(int index)
        {
            onClick.Invoke(index);
        }

        private void GetNewObject()
        {
            var bindingNode = useObjectPooling ?
                objectPool.Get() :
                Instantiate(prefab, transform).GetComponent<BindingNode>();

            bindingNodes.Add(bindingNode);
        }

        public void SetCollection(IEnumerable<object> collection)
        {
            Collection = collection;
        }

        public void UpdateCollection()
        {
            if (Collection == null)
                return;

            var enumerator = Collection.GetEnumerator();

            int i = 0;
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (i >= Count)
                    GetNewObject();

                var bindingNode = this[i];

                bindingNode.gameObject.SetActive(true);

                var bindingTr = bindingNode.transform;

                if (placement == ChildPlacement.None)
                {
                    bindingTr.SetSiblingIndex(i);
                }
                else
                {
                    bindingTr.SetParent(transform.GetChild(i));
                    bindingTr.localPosition = Vector3.zero;
                }

                bindingNode.Binding = current;

                i++;
            }

            for (var j = Count - 1; j >= i; j--)
            {
                var bindingNode = this[j];

                if (useObjectPooling)
                    objectPool.Release(bindingNode);
                else
                    bindingNode.gameObject.SetActive(false);

                bindingNodes.RemoveAt(j);
            }

            if (transform is RectTransform rectTransform)
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }
}

public enum ChildPlacement
{
    None,
    OffsettedChildren
}
