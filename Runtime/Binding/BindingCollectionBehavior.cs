using System.Collections;
using System.Collections.Generic;
using Bodardr.ObjectPooling;
using Bodardr.Utility.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bodardr.Databinding.Runtime
{
    public class BindingCollectionBehavior : MonoBehaviour, ICollectionCallback
    {
        private List<BindingBehavior> bindingBehaviors = new();

        private IEnumerable collection;
        private bool initialized = false;

        private List<PoolableComponent<BindingBehavior>> pooledBindingBehaviors = new();

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

        [ShowIf(nameof(useObjectPooling))]
        [SerializeField]
        private ScriptableObjectPool pool;

        [Header("Children placement")]
        [SerializeField]
        private ChildPlacement placement = ChildPlacement.None;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<int> onClick;

        public BindingBehavior this[int index] =>
            useObjectPooling ? pooledBindingBehaviors[index].Content : bindingBehaviors[index];

        public int Count => useObjectPooling ? pooledBindingBehaviors.Count : bindingBehaviors.Count;

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

            bindingBehaviors.Clear();
            pooledBindingBehaviors.Clear();

            if (!useObjectPooling)
            {
                var presentBindings = GetComponentsInChildren<BindingBehavior>(true);
                bindingBehaviors.AddRange(presentBindings);
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

            foreach (var pooledBehavior in pooledBindingBehaviors)
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

                bindingBehavior.Binding = current;

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