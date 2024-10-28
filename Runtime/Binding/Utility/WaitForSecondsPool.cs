using System.Collections.Generic;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{

    internal static class WaitForSecondsPool
    {
        private static readonly Dictionary<float, WaitForSeconds> pool = new();

        public static WaitForSeconds Get(float duration)
        {
            if (pool.TryGetValue(duration, out var value))
                return value;

            var wait = new WaitForSeconds(duration);
            pool.Add(duration, wait);

            return wait;
        }
    }
}
