using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Core.Zones;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class ZoneAlertLevelsTests
    {
        [Test]
        public void Raise_AccumulatesAndClampsAtMaximum()
        {
            var levels = new ZoneAlertLevels();

            levels.Raise(AlertZoneId.Commercial, 30f);
            levels.Raise(AlertZoneId.Commercial, 30f);
            Assert.AreEqual(60f, levels.Level(AlertZoneId.Commercial), 1e-4f);

            levels.Raise(AlertZoneId.Commercial, 500f);
            Assert.AreEqual(ZoneAlertTuning.MaxLevel, levels.Level(AlertZoneId.Commercial), 1e-4f);
        }

        [Test]
        public void Raise_OnUntrackedZone_DoesNothing()
        {
            // ゾーンの外は誰も見ていない。
            var levels = new ZoneAlertLevels();
            levels.Raise(AlertZoneId.None, 50f);

            Assert.AreEqual(0f, levels.Level(AlertZoneId.None), 1e-4f);
        }

        [Test]
        public void AllThreeZones_AreTracked()
        {
            // 「警戒度を追跡しないゾーン」は設けない（第五節）。
            CollectionAssert.AreEquivalent(
                new[] { AlertZoneId.Quiet, AlertZoneId.Residential, AlertZoneId.Commercial },
                ZoneAlertLevels.Zones);
        }

        [Test]
        public void Decay_IsFastestInQuiet_AndSlowestInCommercial()
        {
            var levels = new ZoneAlertLevels();
            foreach (var zone in ZoneAlertLevels.Zones)
            {
                levels.Raise(zone, 80f);
            }

            levels.Decay(10f);

            var quiet = levels.Level(AlertZoneId.Quiet);
            var residential = levels.Level(AlertZoneId.Residential);
            var commercial = levels.Level(AlertZoneId.Commercial);

            Assert.Less(quiet, residential, "静穏は生活より速く冷める。");
            Assert.Less(residential, commercial, "生活は商業より速く冷める。");
        }

        [Test]
        public void Decay_NeverFallsBelowZero()
        {
            var levels = new ZoneAlertLevels();
            levels.Raise(AlertZoneId.Quiet, 5f);

            levels.Decay(1000f);

            Assert.AreEqual(0f, levels.Level(AlertZoneId.Quiet), 1e-4f);
        }

        [Test]
        public void BeginNextDay_DecaysEachZoneByItsSleepAmount()
        {
            var levels = new ZoneAlertLevels();
            foreach (var zone in ZoneAlertLevels.Zones)
            {
                levels.Raise(zone, 100f);
            }

            levels.BeginNextDay();

            var tuning = ZoneAlertTuning.Default;
            Assert.AreEqual(100f - tuning.SleepDecay(AlertZoneId.Quiet), levels.Level(AlertZoneId.Quiet), 1e-4f);
            Assert.AreEqual(100f - tuning.SleepDecay(AlertZoneId.Residential), levels.Level(AlertZoneId.Residential), 1e-4f);
            Assert.AreEqual(100f - tuning.SleepDecay(AlertZoneId.Commercial), levels.Level(AlertZoneId.Commercial), 1e-4f);
        }

        [Test]
        public void CaptureAndRestore_RoundTrips()
        {
            var levels = new ZoneAlertLevels();
            levels.Raise(AlertZoneId.Quiet, 12f);
            levels.Raise(AlertZoneId.Commercial, 47f);

            var restored = new ZoneAlertLevels();
            restored.Restore(levels.CaptureState());

            Assert.AreEqual(12f, restored.Level(AlertZoneId.Quiet), 1e-4f);
            Assert.AreEqual(0f, restored.Level(AlertZoneId.Residential), 1e-4f);
            Assert.AreEqual(47f, restored.Level(AlertZoneId.Commercial), 1e-4f);
        }

        [Test]
        public void Restore_TreatsStateAsUntrustedInput()
        {
            var levels = new ZoneAlertLevels();
            levels.Raise(AlertZoneId.Quiet, 90f);

            var state = new ZoneAlertState();
            state.Zones.Add(new ZoneAlertEntry(AlertZoneId.Commercial, 500f));   // 範囲外はクランプ
            state.Zones.Add(new ZoneAlertEntry(AlertZoneId.None, 50f));          // 未追跡のゾーンは無視
            state.Zones.Add(new ZoneAlertEntry(AlertZoneId.Residential, -20f));  // 負値はクランプ

            levels.Restore(state);

            Assert.AreEqual(0f, levels.Level(AlertZoneId.Quiet), 1e-4f, "記載のないゾーンは 0 に戻す。");
            Assert.AreEqual(ZoneAlertTuning.MaxLevel, levels.Level(AlertZoneId.Commercial), 1e-4f);
            Assert.AreEqual(0f, levels.Level(AlertZoneId.Residential), 1e-4f);
            Assert.AreEqual(0f, levels.Level(AlertZoneId.None), 1e-4f);
        }

        [Test]
        public void Changed_FiresOnlyWhenTheLevelActuallyMoves()
        {
            var levels = new ZoneAlertLevels();
            var fired = 0;
            levels.Changed += (_, _) => fired++;

            levels.Raise(AlertZoneId.Quiet, 10f);
            Assert.AreEqual(1, fired);

            levels.Raise(AlertZoneId.Quiet, 0f);
            Assert.AreEqual(1, fired, "0 の上昇では通知しない。");

            levels.Decay(0f);
            Assert.AreEqual(1, fired, "経過 0 秒では通知しない。");
        }
    }

    public sealed class PoliceEscalationTests
    {
        [Test]
        public void StageFor_CrossesTheThreeThresholds()
        {
            Assert.AreEqual(PoliceStage.Unaware, PoliceEscalation.StageFor(0f));
            Assert.AreEqual(PoliceStage.Unaware, PoliceEscalation.StageFor(24.9f));
            Assert.AreEqual(PoliceStage.Noticing, PoliceEscalation.StageFor(PoliceEscalation.NoticingThreshold));
            Assert.AreEqual(PoliceStage.Noticing, PoliceEscalation.StageFor(59.9f));
            Assert.AreEqual(PoliceStage.Warning, PoliceEscalation.StageFor(PoliceEscalation.WarningThreshold));
            Assert.AreEqual(PoliceStage.Warning, PoliceEscalation.StageFor(99.9f));
            Assert.AreEqual(PoliceStage.Pursuing, PoliceEscalation.StageFor(PoliceEscalation.PursuingThreshold));
        }

        [Test]
        public void SpeedMultiplier_IsDrivenOnlyByCommercialAlert()
        {
            // 商業ゾーンの警戒度だけが段階進行の速さを駆動する（第五節）。
            Assert.AreEqual(1f, PoliceEscalation.SpeedMultiplier(AlertZoneId.Commercial, 0f), 1e-4f);
            Assert.AreEqual(2f, PoliceEscalation.SpeedMultiplier(AlertZoneId.Commercial, 100f), 1e-4f);
            Assert.AreEqual(1.5f, PoliceEscalation.SpeedMultiplier(AlertZoneId.Commercial, 50f), 1e-4f);

            Assert.AreEqual(1f, PoliceEscalation.SpeedMultiplier(AlertZoneId.Quiet, 100f), 1e-4f);
            Assert.AreEqual(1f, PoliceEscalation.SpeedMultiplier(AlertZoneId.Residential, 100f), 1e-4f);
        }

        [Test]
        public void Advance_HighCommercialAlert_ReachesWarningSooner()
        {
            const float gain = PoliceEscalation.LoiterGainPerSecond;
            const float seconds = 10f;

            var calm = PoliceEscalation.Advance(0f, gain, PoliceEscalation.SpeedMultiplier(AlertZoneId.Commercial, 0f), seconds);
            var tense = PoliceEscalation.Advance(0f, gain, PoliceEscalation.SpeedMultiplier(AlertZoneId.Commercial, 100f), seconds);

            Assert.AreEqual(40f, calm, 1e-4f);
            Assert.AreEqual(80f, tense, 1e-4f);
            Assert.AreEqual(PoliceStage.Noticing, PoliceEscalation.StageFor(calm));
            Assert.AreEqual(PoliceStage.Warning, PoliceEscalation.StageFor(tense));
        }

        [Test]
        public void Advance_ClampsAtMaximum()
        {
            Assert.AreEqual(PoliceEscalation.MaxSuspicion,
                PoliceEscalation.Advance(90f, 100f, 2f, 10f), 1e-4f);
        }

        [Test]
        public void Relax_CoolsDownAndClampsAtZero()
        {
            Assert.AreEqual(100f - PoliceEscalation.OutOfSightDecayPerSecond,
                PoliceEscalation.Relax(100f, 1f), 1e-4f);
            Assert.AreEqual(0f, PoliceEscalation.Relax(10f, 100f), 1e-4f);
        }

        [Test]
        public void NextStage_OnceStarted_ThePursuitEndsOnlyWhenFullyCooled()
        {
            // 閾値の上を往復させない。追われている状態は、逃げ切って初めて終わる。
            Assert.AreEqual(PoliceStage.Pursuing, PoliceEscalation.NextStage(PoliceStage.Pursuing, 99f));
            Assert.AreEqual(PoliceStage.Pursuing, PoliceEscalation.NextStage(PoliceStage.Pursuing, 1f));
            Assert.AreEqual(PoliceStage.Unaware, PoliceEscalation.NextStage(PoliceStage.Pursuing, 0f));
        }

        [Test]
        public void NextStage_BelowPursuit_FollowsThePlainThresholds()
        {
            Assert.AreEqual(PoliceStage.Warning, PoliceEscalation.NextStage(PoliceStage.Noticing, 70f));
            Assert.AreEqual(PoliceStage.Noticing, PoliceEscalation.NextStage(PoliceStage.Warning, 30f));
            Assert.AreEqual(PoliceStage.Pursuing, PoliceEscalation.NextStage(PoliceStage.Warning, 100f));
        }
    }

    public sealed class ConfiscationTests
    {
        static readonly ItemId Bento = new ItemId("bento");
        static readonly ItemId Bread = new ItemId("bread");
        static readonly ItemId Can = new ItemId("can_aluminum");

        static IItemCatalog Catalog() => new FakeCatalog()
            .Add(new ItemDefinition(Bento, "コンビニ弁当", ItemCategory.Food, slots: 2,
                effect: new VitalsDelta { Hunger = 40f },
                rottenPenalty: new VitalsDelta { Hp = -14f, Sanity = -8f }))
            .Add(new ItemDefinition(Bread, "パン", ItemCategory.Food, slots: 2,
                effect: new VitalsDelta { Hunger = 22f }))
            .Add(new ItemDefinition(Can, "アルミ缶", ItemCategory.Salvage, slots: 1, sellPriceYen: 20));

        [Test]
        public void IsSeizable_OnlyRottenFood()
        {
            var catalog = Catalog();
            catalog.TryGet(Bento, out var bento);
            catalog.TryGet(Can, out var can);

            Assert.IsTrue(Confiscation.IsSeizable(bento, FoodState.Rotten));
            Assert.IsFalse(Confiscation.IsSeizable(bento, FoodState.Stale));
            Assert.IsFalse(Confiscation.IsSeizable(bento, FoodState.Fresh));
            Assert.IsFalse(Confiscation.IsSeizable(can, FoodState.Rotten), "換金廃品には状態がない。取り上げられない。");
            Assert.IsFalse(Confiscation.IsSeizable(null, FoodState.Rotten));
        }

        [Test]
        public void SelectSeized_LeavesFreshFoodAndSalvageAlone()
        {
            var items = new[]
            {
                new ItemInstance(Bread, FoodState.Fresh),
                new ItemInstance(Bento, FoodState.Rotten),
                new ItemInstance(Can),
                new ItemInstance(Bread, FoodState.Stale),
            };

            var seized = Confiscation.SelectSeized(items, Catalog(), new ScriptedRng(0d));

            CollectionAssert.AreEqual(new[] { 1 }, seized);
        }

        [Test]
        public void SelectSeized_TakesHalfRoundedUp()
        {
            var items = new[]
            {
                new ItemInstance(Bento, FoodState.Rotten),
                new ItemInstance(Bento, FoodState.Rotten),
                new ItemInstance(Bento, FoodState.Rotten),
            };

            // 候補 3 個 → 端数切り上げで 2 個。「一部」であって全部ではない。
            var seized = Confiscation.SelectSeized(items, Catalog(), new ScriptedRng(0d, 0.9d));

            Assert.AreEqual(2, seized.Count);
        }

        [Test]
        public void SelectSeized_ReturnsDescendingIndices()
        {
            var items = new[]
            {
                new ItemInstance(Bento, FoodState.Rotten),
                new ItemInstance(Bento, FoodState.Rotten),
                new ItemInstance(Bento, FoodState.Rotten),
            };

            var seized = Confiscation.SelectSeized(items, Catalog(), new ScriptedRng(0d, 0.9d));

            // 降順なら、この順に TryRemoveAt を呼んでも索引がずれない。
            CollectionAssert.AreEqual(new[] { 2, 0 }, seized);
        }

        [Test]
        public void SelectSeized_NothingRotten_SeizesNothing()
        {
            var items = new[]
            {
                new ItemInstance(Bread, FoodState.Fresh),
                new ItemInstance(Can),
            };

            Assert.AreEqual(0, Confiscation.SelectSeized(items, Catalog(), new ScriptedRng(0d)).Count);
        }

        [Test]
        public void SelectSeized_UnknownItem_IsIgnored()
        {
            var items = new[] { new ItemInstance(new ItemId("does_not_exist"), FoodState.Rotten) };

            Assert.AreEqual(0, Confiscation.SelectSeized(items, Catalog(), new ScriptedRng(0d)).Count);
        }
    }

    public sealed class SleepDisturbanceTests
    {
        [Test]
        public void Probability_RisesWithQuietAlert()
        {
            Assert.AreEqual(0f, SleepDisturbance.Probability(0f), 1e-4f);
            Assert.AreEqual(SleepDisturbance.MaxProbability * 0.5f, SleepDisturbance.Probability(50f), 1e-4f);
            Assert.AreEqual(SleepDisturbance.MaxProbability, SleepDisturbance.Probability(100f), 1e-4f);
        }

        [Test]
        public void Probability_IsClampedAtBothEnds()
        {
            Assert.AreEqual(0f, SleepDisturbance.Probability(-10f), 1e-4f);
            Assert.AreEqual(SleepDisturbance.MaxProbability, SleepDisturbance.Probability(999f), 1e-4f);
        }

        [Test]
        public void Roll_NeverWakesWhenNobodyKnowsYourFace()
        {
            Assert.IsFalse(SleepDisturbance.Roll(0f, new ScriptedRng(0d)));
        }

        [Test]
        public void Roll_MaxAlert_StillLeavesRoomToSleep()
        {
            // 警戒度が満杯でも必ず起こされるわけではない。
            Assert.IsTrue(SleepDisturbance.Roll(100f, new ScriptedRng(0.5d)));
            Assert.IsFalse(SleepDisturbance.Roll(100f, new ScriptedRng(0.95d)));
        }
    }
}
