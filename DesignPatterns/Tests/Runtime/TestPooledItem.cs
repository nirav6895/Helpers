using UnityEngine;

namespace NG.DesignPatterns.Tests
{
    public class TestPooledItem : MonoBehaviour, IPoolable
    {
        public int Spawns;
        public int Despawns;

        public void OnSpawn() => Spawns++;

        public void OnDespawn() => Despawns++;
    }
}
