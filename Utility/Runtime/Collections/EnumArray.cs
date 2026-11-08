#region Using
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// A serializable collection with exactly one value per member of <typeparamref name="TEnum"/>, e.g. a damage value
    /// per weapon type:
    /// <code>
    /// [SerializeField] private EnumArray&lt;WeaponType, float&gt; damage;
    ///
    /// float swordDamage = damage[WeaponType.Sword];
    /// </code>
    /// <para>
    /// The Inspector shows one field per member, labelled with its name. Values are saved together with their enum value,
    /// so adding, removing or reordering members keeps the other members' values; a removed member's value is dropped.
    /// Like any enum field, values follow the member's number: changing a member's number loses its value.
    /// </para>
    /// Members that share a value (aliases) count once. Iterating gives the members in value order.
    /// </summary>
    /// <typeparam name="TEnum">The enum whose members are the keys.</typeparam>
    /// <typeparam name="T">The value type. It must be a type Unity can serialize to show in the Inspector.</typeparam>
    [Serializable]
    public class EnumArray<TEnum, T> : IEnumerable<KeyValuePair<TEnum, T>>, ISerializationCallbackReceiver
        where TEnum : struct, Enum
    {
        #region Variables
        // The EnumArray drawer finds these by name.
        [SerializeField] private TEnum[] keys;
        [SerializeField] private T[] values;
        #endregion


        #region Constructors
        /// <summary>
        /// Creates the array with the default value of <typeparamref name="T"/> for every member.
        /// </summary>
        public EnumArray()
        {
            Sync();
        }

        /// <summary>
        /// Creates the array with <paramref name="value"/> for every member.
        /// </summary>
        public EnumArray(T value) : this()
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = value;
        }

        /// <summary>
        /// Creates the array with a value for each member, e.g. <c>new EnumArray&lt;WeaponType, float&gt;(type =&gt; 10f)</c>.
        /// </summary>
        public EnumArray(Func<TEnum, T> valueForKey) : this()
        {
            if (valueForKey == null)
                throw new ArgumentNullException(nameof(valueForKey));

            for (int i = 0; i < values.Length; i++)
                values[i] = valueForKey(keys[i]);
        }
        #endregion


        #region Properties
        /// <summary>
        /// The members of <typeparamref name="TEnum"/>, in value order. Aliases count once.
        /// </summary>
        public static IReadOnlyList<TEnum> Keys => EnumKeys<TEnum>.Values;

        /// <summary>
        /// The values, in the same order as <see cref="Keys"/>.
        /// </summary>
        public IReadOnlyList<T> Values => values;

        /// <summary>
        /// Number of values: the number of distinct members of <typeparamref name="TEnum"/>.
        /// </summary>
        public int Count => values.Length;

        /// <summary>
        /// The value for <paramref name="key"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="key"/> isn't a member of <typeparamref name="TEnum"/>, e.g. (WeaponType)99.</exception>
        public T this[TEnum key]
        {
            get => values[IndexOf(key)];
            set => values[IndexOf(key)] = value;
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Gets the value for <paramref name="key"/>. Returns false if <paramref name="key"/> isn't a member of <typeparamref name="TEnum"/>.
        /// </summary>
        public bool TryGetValue(TEnum key, out T value)
        {
            int index = EnumKeys<TEnum>.IndexOf(key);
            if (index < 0)
            {
                value = default;
                return false;
            }

            value = values[index];
            return true;
        }

        /// <summary>
        /// Sets every value to <paramref name="value"/>.
        /// </summary>
        public void Fill(T value)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = value;
        }

        /// <summary>
        /// Enumerates the members and their values, in value order. It doesn't allocate in a foreach.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(values);
        }

        IEnumerator<KeyValuePair<TEnum, T>> IEnumerable<KeyValuePair<TEnum, T>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            Sync();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Sync();
        }
        #endregion


        #region Private Methods
        private static int IndexOf(TEnum key)
        {
            int index = EnumKeys<TEnum>.IndexOf(key);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(key), key, $"{key} is not a member of {typeof(TEnum).Name}.");

            return index;
        }

        /// <summary>
        /// Makes <see cref="keys"/> exactly the enum's members, in value order, and moves each saved value to its member.
        /// </summary>
        private void Sync()
        {
            TEnum[] members = EnumKeys<TEnum>.Values;
            if (IsInSync(members))
                return;

            T[] synced = new T[members.Length];

            if (keys != null && values != null)
            {
                bool[] assigned = new bool[members.Length];
                int count = Math.Min(keys.Length, values.Length);
                for (int i = 0; i < count; i++)
                {
                    int index = EnumKeys<TEnum>.IndexOf(keys[i]);
                    if (index < 0 || assigned[index])
                        continue;

                    synced[index] = values[i];
                    assigned[index] = true;
                }
            }
            else if (values != null)
            {
                // No keys saved (e.g. data from a plain array): keep the values in order.
                Array.Copy(values, synced, Math.Min(values.Length, synced.Length));
            }

            keys = (TEnum[])members.Clone();
            values = synced;
        }

        private bool IsInSync(TEnum[] members)
        {
            if (keys == null || values == null || keys.Length != members.Length || values.Length != members.Length)
                return false;

            EqualityComparer<TEnum> comparer = EqualityComparer<TEnum>.Default;
            for (int i = 0; i < members.Length; i++)
            {
                if (!comparer.Equals(keys[i], members[i]))
                    return false;
            }

            return true;
        }
        #endregion


        #region Enumerator
        /// <summary>
        /// Enumerates the members and their values, in value order.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TEnum, T>>
        {
            private readonly T[] values;
            private int index;

            internal Enumerator(T[] values)
            {
                this.values = values;
                index = -1;
            }

            public KeyValuePair<TEnum, T> Current => new KeyValuePair<TEnum, T>(EnumKeys<TEnum>.Values[index], values[index]);

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                return ++index < values.Length;
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
            }
        }
        #endregion
    }
}
