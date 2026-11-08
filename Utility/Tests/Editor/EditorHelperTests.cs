#region Using
using NUnit.Framework;
using NG.Utility.Editor;
#endregion

namespace NG.Utility.Tests
{
    public class EditorHelperTests
    {
        [TestCase("items.Array.data[0]", 0)]
        [TestCase("items.Array.data[3]", 3)]
        [TestCase("outer.Array.data[2].inner.Array.data[15]", 15)]
        [TestCase("items", -1)]
        [TestCase("items.Array.data[]", -1)]
        [TestCase("items.Array.data[x]", -1)]
        [TestCase("", -1)]
        public void GetArrayIndex(string path, int expected)
        {
            Assert.AreEqual(expected, DrawerUtility.GetArrayIndex(path));
        }

        [Test]
        public void EnumKeys_Names_AreInValueOrder_WithAliasesOnce()
        {
            CollectionAssert.AreEqual(new[] { "Sword", "Bow", "Staff" }, EnumKeys.GetNames(typeof(Weapon)));

            string[] rarity = EnumKeys.GetNames(typeof(Rarity));
            Assert.AreEqual(3, rarity.Length);
            Assert.AreEqual("Epic", rarity[0]);
            Assert.AreEqual("Common", rarity[1]);
        }
    }
}
