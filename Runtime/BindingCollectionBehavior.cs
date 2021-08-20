using System.Collections.Generic;
using Bodardr.ObjectPooling;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    public class BindingCollectionBehavior : MonoBehaviour
    {
        [SerializeField]
        private bool setAmount = false;

        [SerializeField]
        private int amount;

        [SerializeField]
        private PrefabPool pool;

        private IEnumerable<object> collection;
    }
}