#region Using
using System.Collections;
using NUnit.Framework;
using UnityEngine;
#endregion

namespace NG.Utility.Tests
{
    internal static class TestUtility
    {
        /// <summary>
        /// Runs a coroutine like Unity does, failing if it takes longer than <paramref name="timeout"/> seconds
        /// instead of hanging the test run.
        /// </summary>
        public static IEnumerator RunWithTimeout(IEnumerator routine, float timeout, string name)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (routine.MoveNext())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"{name} didn't finish within {timeout} seconds.");

                yield return routine.Current;
            }
        }

        /// <summary>
        /// Waits until <paramref name="condition"/> is true, failing after <paramref name="timeout"/> seconds.
        /// </summary>
        public static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string description)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out waiting until {description}.");

                yield return null;
            }
        }
    }
}
