using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Persistence;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;
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

        [Test]
        public void AnUnmigratedOldVersion_IsRejected()
        {
            // 引き上げ前のセーブが検証に届くことはない。届いたなら経路の誤りである。
            Assert.IsFalse(SaveGameValidation.TryValidate(new SaveGame { Version = 1 }, out _));
        }

        [Test]
        public void ZoneAlerts_AreTreatedAsUntrustedInput()
        {
            var unknownZone = new SaveGame();
            unknownZone.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.None, 10f));
            Assert.IsFalse(SaveGameValidation.TryValidate(unknownZone, out var zoneError));
            StringAssert.Contains("未知の警戒ゾーン", zoneError);

            var outOfRange = new SaveGame();
            outOfRange.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Quiet, 101f));
            Assert.IsFalse(SaveGameValidation.TryValidate(outOfRange, out var rangeError));
            StringAssert.Contains("範囲外", rangeError);

            var duplicated = new SaveGame();
            duplicated.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Quiet, 10f));
            duplicated.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Quiet, 20f));
            Assert.IsFalse(SaveGameValidation.TryValidate(duplicated, out var dupError));
            StringAssert.Contains("二度", dupError);

            var missingStructure = new SaveGame { ZoneAlerts = null };
            Assert.IsFalse(SaveGameValidation.TryValidate(missingStructure, out _));
        }

        [Test]
        public void APartialButValidZoneAlertList_IsAccepted()
        {
            // 記載のないゾーンは 0 として扱えるため、全ゾーンが揃っていることまでは求めない。
            var save = new SaveGame();
            save.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Commercial, 42f));

            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var error), error);
        }

        [Test]
        public void Knacks_AreTreatedAsUntrustedInput()
        {
            var unknownKnack = new SaveGame();
            unknownKnack.Knacks.Acquired.Add((KnackId)999);
            Assert.IsFalse(SaveGameValidation.TryValidate(unknownKnack, out var unknownError));
            StringAssert.Contains("未知", unknownError);

            var duplicated = new SaveGame();
            duplicated.Knacks.Acquired.Add(KnackId.SpotDuds);
            duplicated.Knacks.Acquired.Add(KnackId.SpotDuds);
            Assert.IsFalse(SaveGameValidation.TryValidate(duplicated, out var dupError));
            StringAssert.Contains("二度", dupError);

            var negativeCounter = new SaveGame();
            negativeCounter.Knacks.RummageCount = -1;
            Assert.IsFalse(SaveGameValidation.TryValidate(negativeCounter, out var counterError));
            StringAssert.Contains("負", counterError);

            var missingStructure = new SaveGame { Knacks = null };
            Assert.IsFalse(SaveGameValidation.TryValidate(missingStructure, out _));

            var missingCarrySlot = new SaveGame { CarrySlot = null };
            Assert.IsFalse(SaveGameValidation.TryValidate(missingCarrySlot, out _));
        }

        [Test]
        public void AValidPartialKnackState_IsAccepted()
        {
            var save = new SaveGame();
            save.Knacks.Acquired.Add(KnackId.IronStomach);
            save.Knacks.RummageCount = 4;

            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var error), error);
        }
    }

    public sealed class SaveGameMigrationTests
    {
        [Test]
        public void Version1_IsUpgradedWithNobodyKnowingYourFace()
        {
            var save = new SaveGame { Version = 1, ZoneAlerts = null };

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var error), error);
            Assert.AreEqual(SaveGame.CurrentVersion, save.Version);
            Assert.IsNotNull(save.ZoneAlerts);
            Assert.AreEqual(0, save.ZoneAlerts.Zones.Count, "版 1 の世界には警察がまだ存在しなかった。");
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);
        }

        [Test]
        public void ARealVersion1Json_LoadsAsAFaceNobodyKnows()
        {
            // 手元の版 1 のセーブが読めることを、実際の JSON で確かめる。
            const string json =
                "{\"Version\":1," +
                "\"Clock\":{\"Day\":3,\"ElapsedInDay\":120.0}," +
                "\"Vitals\":{\"Hp\":80.0,\"Thirst\":50.0,\"Hunger\":40.0,\"Sanity\":65.0}," +
                "\"Inventory\":{\"Capacity\":6,\"Items\":[]}," +
                "\"WalletYen\":300," +
                "\"SleepSpotId\":\"bench_park\"}";

            var save = UnityEngine.JsonUtility.FromJson<SaveGame>(json);

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var upgradeError), upgradeError);
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);

            Assert.AreEqual(3, save.Clock.Day);
            Assert.AreEqual(300, save.WalletYen);
            Assert.AreEqual("bench_park", save.SleepSpotId);

            var levels = new ZoneAlertLevels();
            levels.Restore(save.ZoneAlerts);
            foreach (var zone in ZoneAlertLevels.Zones)
            {
                Assert.AreEqual(0f, levels.Level(zone), 1e-4f);
            }
        }

        [Test]
        public void Version2_IsUpgradedWithNoKnacksYetLearned()
        {
            var save = new SaveGame { Version = 2, Knacks = null };
            save.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Commercial, 30f));

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var error), error);
            Assert.AreEqual(SaveGame.CurrentVersion, save.Version);
            Assert.IsNotNull(save.Knacks);
            Assert.AreEqual(0, save.Knacks.Acquired.Count, "版 2 の世界にはコツがまだ存在しなかった。");
            Assert.AreEqual(30f, save.ZoneAlerts.Zones[0].Level, 1e-4f, "警戒度は引き上げで失われない。");
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);
        }

        [Test]
        public void Version3_IsUpgradedWithNothingOnYourBack()
        {
            var save = new SaveGame { Version = 3, CarrySlot = null };
            save.Knacks.Acquired.Add(KnackId.IronStomach);

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var error), error);
            Assert.AreEqual(SaveGame.CurrentVersion, save.Version);
            Assert.IsNotNull(save.CarrySlot);
            Assert.IsFalse(save.CarrySlot.Occupied, "版 3 の世界では誰も段ボールを担いでいなかった。");
            Assert.AreEqual(1, save.Knacks.Acquired.Count, "コツは引き上げで失われない。");
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);
        }

        [Test]
        public void AVersion1Json_IsCarriedAllTheWayToTheCurrentVersion()
        {
            // 版 1 の JSON がコツの版まで一気に引き上がることを確かめる。
            const string json =
                "{\"Version\":1," +
                "\"Clock\":{\"Day\":2,\"ElapsedInDay\":60.0}," +
                "\"Vitals\":{\"Hp\":80.0,\"Thirst\":50.0,\"Hunger\":40.0,\"Sanity\":65.0}," +
                "\"Inventory\":{\"Capacity\":6,\"Items\":[]}," +
                "\"WalletYen\":100," +
                "\"SleepSpotId\":\"bench_park\"}";

            var save = UnityEngine.JsonUtility.FromJson<SaveGame>(json);

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var upgradeError), upgradeError);
            Assert.AreEqual(SaveGame.CurrentVersion, save.Version);
            Assert.IsNotNull(save.Knacks);
            Assert.AreEqual(0, save.Knacks.Acquired.Count);
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);
        }

        [Test]
        public void AFutureVersion_IsRefusedRatherThanGuessedAt()
        {
            var save = new SaveGame { Version = SaveGame.CurrentVersion + 1 };

            Assert.IsFalse(SaveGameMigration.TryUpgrade(save, out var error));
            StringAssert.Contains("新しいセーブデータ", error);
        }

        [Test]
        public void AVersionBelowTheOldestSupported_IsRefused()
        {
            Assert.IsFalse(SaveGameMigration.TryUpgrade(new SaveGame { Version = 0 }, out _));
            Assert.IsFalse(SaveGameMigration.TryUpgrade(null, out _));
        }

        [Test]
        public void TheCurrentVersion_PassesThroughUntouched()
        {
            var save = new SaveGame();
            save.ZoneAlerts.Zones.Add(new ZoneAlertEntry(AlertZoneId.Quiet, 12f));

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out _));
            Assert.AreEqual(1, save.ZoneAlerts.Zones.Count);
            Assert.AreEqual(12f, save.ZoneAlerts.Zones[0].Level, 1e-4f);
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
