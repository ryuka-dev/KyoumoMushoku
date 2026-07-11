using KyoumoMushoku.Core.Items;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 保管庫（段ボール箱）の器としての振る舞い（第十二節）。出し入れの制限は空きマスだけで、
    /// 背負い物は器に詰められない。セーブの往復で中身・種別・容量が保たれる。
    /// </summary>
    public sealed class StashTests
    {
        static Stash NewStash(int capacity) =>
            new Stash(TestItems.Catalog(), StashKind.CardboardBox, "stash_test", capacity);

        [Test]
        public void Deposit_FillsUntilCapacity_ThenRefuses()
        {
            var stash = NewStash(3);

            Assert.IsTrue(stash.TryDeposit(new ItemInstance(TestItems.Onigiri)));      // 1マス
            Assert.IsTrue(stash.TryDeposit(new ItemInstance(TestItems.WaterBottle)));  // +2 = 3マス
            Assert.AreEqual(3, stash.UsedSlots);

            Assert.IsFalse(stash.CanDeposit(new ItemInstance(TestItems.Onigiri)), "満杯の箱には入らない。");
            Assert.IsFalse(stash.TryDeposit(new ItemInstance(TestItems.Onigiri)));
            Assert.AreEqual(2, stash.Count);
        }

        [Test]
        public void Cardboard_CannotBeStored_BecauseItIsCarriedOnBack()
        {
            var stash = NewStash(12);

            // 段ボールは器に詰める物ではなく担ぐ物（第十一節）。箱の中には入らない。
            Assert.IsFalse(stash.CanDeposit(new ItemInstance(TestItems.Cardboard)));
            Assert.IsFalse(stash.TryDeposit(new ItemInstance(TestItems.Cardboard)));
            Assert.AreEqual(0, stash.Count);
        }

        [Test]
        public void Withdraw_RemovesAndReturnsTheItemWithItsFreshness()
        {
            var stash = NewStash(12);
            stash.TryDeposit(new ItemInstance(TestItems.Onigiri));
            stash.TryDeposit(new ItemInstance(TestItems.Bento, FoodState.Rotten));

            Assert.IsTrue(stash.TryWithdrawAt(1, out var taken));
            Assert.AreEqual(TestItems.Bento, taken.ItemId);
            Assert.AreEqual(FoodState.Rotten, taken.Freshness, "取り出しても状態はそのまま持ち出せる。");
            Assert.AreEqual(1, stash.Count);
        }

        [Test]
        public void CaptureAndRestore_PreserveContentsKindAndCapacity()
        {
            var stash = NewStash(8);
            stash.TryDeposit(new ItemInstance(TestItems.Onigiri));
            stash.TryDeposit(new ItemInstance(TestItems.WaterBottle));

            var state = stash.CaptureState();
            Assert.AreEqual("stash_test", state.SpotId);
            Assert.AreEqual(StashKind.CardboardBox, state.Kind);
            Assert.AreEqual(8, state.Capacity);
            Assert.AreEqual(2, state.Items.Count);

            var restored = new Stash(TestItems.Catalog(), state.Kind, state.SpotId, state.Capacity);
            Assert.AreEqual(0, restored.Restore(state), "収まる中身は1つも捨てない。");
            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual(8, restored.Capacity);
        }

        [Test]
        public void Restore_DropsItemsThatNoLongerFit()
        {
            // 器が小さくなって復元すると、収まらない分は黙って捨て、捨てた個数を返す（外部入力として扱う）。
            var big = NewStash(12);
            big.TryDeposit(new ItemInstance(TestItems.WaterBottle)); // 2
            big.TryDeposit(new ItemInstance(TestItems.WaterBottle)); // +2 = 4

            var state = big.CaptureState();
            state.Capacity = 2;

            var restored = new Stash(TestItems.Catalog(), state.Kind, state.SpotId, state.Capacity);
            Assert.AreEqual(1, restored.Restore(state));
            Assert.AreEqual(2, restored.UsedSlots);
        }
    }
}
