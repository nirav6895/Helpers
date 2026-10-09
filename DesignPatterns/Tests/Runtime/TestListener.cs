using UnityEngine;

namespace NG.DesignPatterns.Tests
{
    /// <summary>
    /// Listener on a GameObject, used to check that methods of destroyed objects are removed.
    /// </summary>
    public class TestListener : MonoBehaviour
    {
        public int Calls;

        public void OnPing<T>(T _) => Calls++;

        public void OnPaused() => Calls++;
    }
}
