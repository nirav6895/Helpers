#region Using
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// A <see cref="Dictionary{TKey, TValue}"/> that Unity can serialize and show in the Inspector:
    /// <code>
    /// [SerializeField] private SerializableDictionary&lt;string, int&gt; prices;
    ///
    /// int price = prices["sword"];
    /// </code>
    /// <para>
    /// In the Inspector it's a list of key/value rows. Rows with a duplicate key, or an empty object reference as key,
    /// are highlighted and ignored: the first row with a key wins. Once code changes the dictionary, the rows are
    /// rewritten from it and the ignored rows are dropped.
    /// </para>
    /// Keys and values must be types Unity can serialize. Main thread only, like Dictionary.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        #region Serialized Types
        // The drawer finds the fields "entries", "key" and "value" by name.
        [Serializable]
        internal struct Entry
        {
            public TKey key;
            public TValue value;

            public Entry(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }
        #endregion


        #region Variables
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Code changed the dictionary since it was read from <see cref="entries"/>, so the entries must be rewritten.
        /// Until then the entries are kept as they are, including duplicate keys being edited in the Inspector.
        /// </summary>
        private bool entriesOutdated;
        #endregion


        #region Properties
        private Dictionary<TKey, TValue> Dictionary => dictionary ??= new Dictionary<TKey, TValue>();

        public int Count => Dictionary.Count;

        public Dictionary<TKey, TValue>.KeyCollection Keys => Dictionary.Keys;

        public Dictionary<TKey, TValue>.ValueCollection Values => Dictionary.Values;

        public TValue this[TKey key]
        {
            get => Dictionary[key];
            set
            {
                Dictionary[key] = value;
                entriesOutdated = true;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Dictionary.Keys;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Dictionary.Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Dictionary.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Dictionary.Values;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        #endregion


        #region Public Methods
        public void Add(TKey key, TValue value)
        {
            Dictionary.Add(key, value);
            entriesOutdated = true;
        }

        public bool Remove(TKey key)
        {
            if (!Dictionary.Remove(key))
                return false;

            entriesOutdated = true;
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public void Clear()
        {
            Dictionary.Clear();
            entriesOutdated = true;
        }

        /// <summary>
        /// Enumerates the pairs. It doesn't allocate in a foreach.
        /// </summary>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Remove(item))
                return false;

            entriesOutdated = true;
            return true;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!entriesOutdated)
                return;

            entriesOutdated = false;
            entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in Dictionary)
                entries.Add(new Entry(pair.Key, pair.Value));
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            entriesOutdated = false;
            entries ??= new List<Entry>();

            Dictionary.Clear();
            foreach (Entry entry in entries)
            {
                // Dictionary can't take null keys; duplicates keep the first row.
                if (entry.key == null || Dictionary.ContainsKey(entry.key))
                    continue;

                Dictionary.Add(entry.key, entry.value);
            }
        }
        #endregion
    }
}
