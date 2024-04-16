using System.Collections;
using System.Collections.Generic;
using Bodardr.ObjectPooling;
using Bodardr.Utility.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Bodardr.Databinding.Runtime
{
    public class BindingCollectionBehavior : MonoBehaviour, ICollectionCallback
    {
        private List<BindingNode> bindingNodes = new();

        private IEnumerable collection;
        private bool initialized = false;

        private List<PoolableComponent<BindingNode>> pooledBindingNodes = new();

        [Header("Instantiation")]
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

        [FormerlySerializedAs("pool")]
        [ShowIf(nameof(useObjectPooling))]
        [SerializeField]
        private ScriptableObjectPrefabPool prefabPool;

        [Header("Children placement")]
        [SerializeField]
        private ChildPlacement placement = ChildPlacement.None;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<int> onClick;

        public BindingNode this[int index] =>
            useObjectPooling ? pooledBindingNodes[index].Content : bindingNodes[index];

        public int Count => useObjectPooling ? pooledBindingNodes.Count : bindingNodes.Count;

        public IEnumerable Collection
        {
            get => collection;
            set
            {
                if (!initialized)
                    Awake();

                collection = value;
                UpdateCollection();
            }
        }

        private void Awake()
        {
            if (initialized)
                return;

            bindingNodes.Clear();
            pooledBindingNodes.Clear();

            if (!useObjectPooling)
            {
                var presentBindings = GetComponentsInChildren<BindingNode>(true);
                bindingNodes.AddRange(presentBindings);
            }

            if (!setAmount)
                return;

            for (int i = 0; i < amount; i++)
                GetNewObject();

            initialized = true;
        }

        private void OnDestroy()
        {
            if (!useObjectPooling)
                return;

            foreach (var pooledBehavior in pooledBindingNodes)
                pooledBehavior.Release();
        }

        public void OnItemClicked(int index)
        {
            onClick.Invoke(index);
        }

        private void GetNewObject()
        {
            if (useObjectPooling)
            {
                var bindingNode = prefabPool.Get<BindingNode>();
                bindingNode.Content.transform.SetParent(transform);
                pooledBindingNodes.Add(bindingNode);
            }
            else
            {
                var bindingNode = Instantiate(prefab, transform).GetComponent<BindingNode>();
                bindingNodes.Add(bindingNode);
            }
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
            bool isDynamic = false;

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (i == 0)
                {
                    isDynamic = current.GetType().GetInterface("INotifyPropertyChanged") != null;
                }

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

            for (var j = i; j < Count; j++)
                this[j].gameObject.SetActive(false);

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