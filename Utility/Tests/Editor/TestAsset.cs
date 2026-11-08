#region Using
using UnityEngine;
#endregion

namespace NG.Utility.Tests
{
    /// <summary>
    /// A ScriptableObject with the serializable collections, to read them through SerializedObject like the drawers do.
    /// </summary>
    public class TestAsset : ScriptableObject
    {
        public EnumArray<Weapon, int> damage = new EnumArray<Weapon, int>();
        public SerializableDictionary<string, int> prices = new SerializableDictionary<string, int>();
    }
}
