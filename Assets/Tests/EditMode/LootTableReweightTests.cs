using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 表の重みを掛け直す（切片の SAN の保底・`Docs/切片の品目.md` 第十節）の検証。
    /// 空振りの重みは変えず、元の表も変えない。
    /// </summary>
    public sealed class LootTableReweightTests
    {
        static readonly ItemId A = new ItemId("a");
        static readonly ItemId B = new ItemId("b");
        static readonly ItemId C = new ItemId("c");

        // 空振り 2・A 1・B 1（合計 4）
        static LootTable Base() => new LootTable(
            new[] { new LootTable.Entry(A, 1f), new LootTable.Entry(B, 1f) }, emptyWeight: 2f, default);

        [Test]
        public void Scale_RaisesOnlyTheChosenItem()
        {
            // B を 3 倍：空振り [0,2)・A [2,3)・B [3,6)
            var table = Base().Reweighted(item => item == B ? 3f : 1f);
            Assert.IsFalse(table.Draw(new ScriptedRng(0.30d), null).HasItem, "1.8 は空振り。空振りの重みは変わらない。");
            Assert.AreEqual(A, table.Draw(new ScriptedRng(0.45d), null).Item); // 2.7
            Assert.AreEqual(B, table.Draw(new ScriptedRng(0.60d), null).Item); // 3.6
        }

        [Test]
        public void Original_IsUnchanged()
        {
            var original = Base();
            original.Reweighted(item => item == B ? 3f : 1f);
            Assert.AreEqual(A, original.Draw(new ScriptedRng(0.60d), null).Item, "元の表（合計 4）では 2.4 は A。");
        }

        [Test]
        public void Extra_AddsCandidates()
        {
            // C 2 を足す：空振り [0,2)・A [2,3)・B [3,4)・C [4,6)
            var table = Base().Reweighted(null, new[] { new LootTable.Entry(C, 2f) });
            Assert.AreEqual(C, table.Draw(new ScriptedRng(0.90d), null).Item); // 5.4
            Assert.AreEqual(B, table.Draw(new ScriptedRng(0.60d), null).Item); // 3.6
        }

        [Test]
        public void NegativeScale_IsTreatedAsZero()
        {
            // A を負の倍率で消す：空振り [0,2)・B [2,3)
            var table = Base().Reweighted(item => item == A ? -1f : 1f);
            Assert.AreEqual(B, table.Draw(new ScriptedRng(0.80d), null).Item); // 2.4
        }
    }
}
