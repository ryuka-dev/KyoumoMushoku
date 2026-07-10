using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Persistence;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class SaveGameValidationTests
    {
        [Test]
        public void AFreshSaveGame_IsValid()
        {
            Assert.IsTrue(SaveGameValidation.TryValidate(new SaveGame(), out var error), error);
        }

        [Test]
        public void NullOrStructurallyBroken_IsRejected()
        {
            Assert.IsFalse(SaveGameValidation.TryValidate(null, out _));
            Assert.IsFalse(SaveGameValidation.TryValidate(new SaveGame { Clock = null }, out _));
            Assert.IsFalse(SaveGameValidation.TryValidate(new SaveGame { Vitals = null }, out _));
            Assert.IsFalse(SaveGameValidation.TryValidate(new SaveGame { Inventory = null }, out _));
        }

        [Test]
        public void AnIncompatibleVersion_IsRejectedExplicitly()
        {
            var save = new SaveGame { Version = SaveGame.CurrentVersion + 1 };

            Assert.IsFalse(SaveGameValidation.TryValidate(save, out var error));
            StringAssert.Contains("バージョン", error);
        }

        [Test]
        public void NonsenseNumbers_AreRejected()
        {
            var zeroDay = new SaveGame();
            zeroDay.Clock.Day = 0;
            Assert.IsFalse(SaveGameValidation.TryValidate(zeroDay, out _));

            var nanSanity = new SaveGame();
            nanSanity.Vitals.Sanity = float.NaN;
            Assert.IsFalse(SaveGameValidation.TryValidate(nanSanity, out _));

            var rewoundClock = new SaveGame();
            rewoundClock.Clock.ElapsedInDay = -1f;
            Assert.IsFalse(SaveGameValidation.TryValidate(rewoundClock, out _));
        }

        [Test]
        public void NegativeMoney_IsRejected_BecauseDebtDoesNotExist()
        {
            Assert.IsFalse(SaveGameValidation.TryValidate(new SaveGame { WalletYen = -1 }, out _));
        }
    }

    public sealed class WalletTests
    {
        [Test]
        public void TrySpend_RefusesWhatItCannotAfford()
        {
            var wallet = new Wallet(100);

            Assert.IsFalse(wallet.TrySpend(101));
            Assert.AreEqual(100, wallet.Yen);

            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(0, wallet.Yen);
        }

        [Test]
        public void SeizeUpTo_TakesEverythingButNeverCreatesDebt()
        {
            var poor = new Wallet(200);
            Assert.AreEqual(200, poor.SeizeUpTo(500), "所持金が医療費に満たなければ全額没収。");
            Assert.AreEqual(0, poor.Yen);

            var rich = new Wallet(2000);
            Assert.AreEqual(500, rich.SeizeUpTo(500));
            Assert.AreEqual(1500, rich.Yen);
        }

        [Test]
        public void NegativeAmounts_AreIgnored()
        {
            var wallet = new Wallet(50);
            wallet.Add(-10);
            Assert.AreEqual(50, wallet.Yen);

            Assert.IsFalse(wallet.TrySpend(-10));
            Assert.AreEqual(0, wallet.SeizeUpTo(-10));
            Assert.AreEqual(50, wallet.Yen);
        }

        [Test]
        public void Changed_FiresOnlyWhenMoneyActuallyMoves()
        {
            var wallet = new Wallet(50);
            var changes = 0;
            wallet.Changed += () => changes++;

            wallet.Add(0);
            wallet.TrySpend(0);
            wallet.SeizeUpTo(0);
            Assert.AreEqual(0, changes);

            wallet.Add(10);
            wallet.TrySpend(10);
            Assert.AreEqual(2, changes);
        }
    }
}
