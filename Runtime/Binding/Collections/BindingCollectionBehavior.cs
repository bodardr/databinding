using System.Collections;
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
        private List<BindingNode> bindingNodes = new();

        private IEnumerable collection;
        private bool initialized = false;

        private ObjectPool<BindingNode> objectPool;
        private List<BindingNode> activelyPooledObjects = new();

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

        public BindingNode this[int index] =>
            useObjectPooling ? activelyPooledObjects[index] : bindingNodes[index];

        public int Count => useObjectPooling ? objectPool.CountActive : bindingNodes.Count;

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

            bindingNodes.Clear();
            objectPool.Clear();

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

            foreach (var bindingNode in activelyPooledObjects)
                objectPool.Release(bindingNode);

            activelyPooledObjects.Clear();
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

            for (var j = i; j < Count; j++)
            {
                var bindingNode = this[j];

                if (useObjectPooling)
                {
                    objectPool.Release(bindingNode);
                    activelyPooledObjects.Remove(bindingNode);
                }
                else
                {
                    bindingNode.gameObject.SetActive(false);
                }
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
