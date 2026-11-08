#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for lists and arrays.
    /// The overloads without a <see cref="Random"/> use <see cref="UnityEngine.Random"/>, so they are main thread only.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Shuffles the list in place (Fisher-Yates), using <see cref="UnityEngine.Random"/>.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            for (int i = list.Count - 1; i > 0; i--)
                Swap(list, i, UnityEngine.Random.Range(0, i + 1));
        }

        /// <summary>
        /// Shuffles the list in place (Fisher-Yates), using <paramref name="random"/>, e.g. a seeded one for repeatable results.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list, Random random)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            for (int i = list.Count - 1; i > 0; i--)
                Swap(list, i, random.Next(i + 1));
        }

        /// <summary>
        /// Returns a random element, using <see cref="UnityEngine.Random"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The list is empty.</exception>
        public static T GetRandom<T>(this IReadOnlyList<T> list)
        {
            CheckNotEmpty(list);
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Returns a random element, using <paramref name="random"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The list is empty.</exception>
        public static T GetRandom<T>(this IReadOnlyList<T> list, Random random)
        {
            CheckNotEmpty(list);
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            return list[random.Next(list.Count)];
        }

        private static void Swap<T>(IList<T> list, int a, int b)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        private static void CheckNotEmpty<T>(IReadOnlyList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (list.Count == 0)
                throw new InvalidOperationException("Can't get a random element from an empty list.");
        }
    }
}
