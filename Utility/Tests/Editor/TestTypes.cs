#region Using
using System;
#endregion

namespace NG.Utility.Tests
{
    public enum Weapon
    {
        Sword,
        Bow,
        Staff
    }

    /// <summary>
    /// Not numbered 0, 1, 2, with an alias, to test value order and duplicate values.
    /// </summary>
    public enum Rarity
    {
        Common = 10,
        Rare = 20,
        Epic = 5,
        Legendary = 20
    }

    public enum SmallEnum : byte
    {
        A,
        B,
        C
    }

    public enum LongEnum : long
    {
        A,
        B,
        C
    }

    /// <summary>
    /// Has a negative value, so it isn't numbered 0, 1, 2 and uses the dictionary lookup.
    /// </summary>
    public enum SignedEnum
    {
        Minus = -1,
        Zero = 0,
        One = 1
    }

    [Serializable]
    public class EnumArrayHolder
    {
        public EnumArray<Weapon, int> damage = new EnumArray<Weapon, int>();
    }

    [Serializable]
    public class DictionaryHolder
    {
        public SerializableDictionary<string, int> prices = new SerializableDictionary<string, int>();
    }

    [Serializable]
    public class PrefsData
    {
        public string name;
        public int level;
    }
}
