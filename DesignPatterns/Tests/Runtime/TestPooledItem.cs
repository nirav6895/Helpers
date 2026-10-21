using UnityEngine;

namespace NG.DesignPatterns.Tests
{
    public class TestPooledItem : MonoBehaviour, IPoolable
    {
        public int Spawns;
        public int Despawns;

        /// <summary>
        /// When set, the item releases itself to this pool in OnDisable, like `void OnDisable() => pool.Release(this)`.
        /// </summary>
        public TestPool ReleaseOnDisable;

        public void OnSpawn() => Spawns++;

        public void OnDespawn() => Despawns++;

        private void OnDisable()
        {
            if (ReleaseOnDisable != null)
                ReleaseOnDisable.Release(this);
        }
    }
}
