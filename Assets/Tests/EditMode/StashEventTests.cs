using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 保管庫イベントの抽選（第十二節）。乱数源を差し替えて決定的に確かめる。空の保管庫は脅かされない。
    /// 発生確率は警戒度と保管量の両方から作り、種別は主因の重みで引く。損失は種別で異なる。
    /// </summary>
    public sealed class StashEventTests
    {
        /// <summary>台本どおりの値を順に返す乱数源。尽きたら最後の値を返し続ける。</summary>
        sealed class ScriptedRng : IRng
        {
            readonly Queue<double> _values;
            double _last;

            public ScriptedRng(params double[] values)
            {
                _values = new Queue<double>(values);
            }

            public double NextDouble()
            {
                if (_values.Count > 0)
                {
                    _last = _values.Dequeue();
                }

                return _last;
            }
        }

        [Test]
        public void AnEmptyStash_IsNeverThreatened()
        {
            // 膨らんでいない箱は狙われない。第一引数がどれだけ低くても（＝必ず発生の側でも）None。
            Assert.AreEqual(StashEventKind.None, StashEventRoll.Roll(new ScriptedRng(0.0), 100f, 0));
        }

        [Test]
        public void WhenTheProbabilityRollExceedsTheChance_NothingHappens()
        {
            // 1つ目の抽選が高ければ発生しない。
            Assert.AreEqual(StashEventKind.None, StashEventRoll.Roll(new ScriptedRng(0.99), 100f, 8));
        }

        [Test]
        public void FullnessAlone_YieldsScavengedByPeers()
        {
            // 警戒度0・保管量ありなら、重みは同業者だけに乗る。1つ目の抽選は発生の側に置く。
            var kind = StashEventRoll.Roll(new ScriptedRng(0.0, 0.5), residentialAlert: 0f, usedSlots: 6);
            Assert.AreEqual(StashEventKind.ScavengedByPeers, kind);
        }

        [Test]
        public void HighAlert_CanYieldCityCleaningOrPoliceRemoval_ByTheSecondDraw()
        {
            // 高警戒度では 清掃・撤去 の重みが立つ。2つ目の抽選の位置で種別が決まる。
            var cleaning = StashEventRoll.Roll(new ScriptedRng(0.0, 0.1), residentialAlert: 100f, usedSlots: 1);
            Assert.AreEqual(StashEventKind.CityCleaning, cleaning);

            var police = StashEventRoll.Roll(new ScriptedRng(0.0, 0.95), residentialAlert: 100f, usedSlots: 1);
            Assert.AreEqual(StashEventKind.PoliceRemoval, police);
        }

        [Test]
        public void PoliceRemoval_NeverAppearsBelowTheAlertFloor()
        {
            // 警戒度が低いうちは撤去は候補に入らない（顔をまだ覚えられていない）。何度引いても撤去は出ない。
            for (var i = 0; i < 20; i++)
            {
                var kind = StashEventRoll.Roll(new ScriptedRng(0.0, i / 20.0), residentialAlert: 30f, usedSlots: 6);
                Assert.AreNotEqual(StashEventKind.PoliceRemoval, kind);
            }
        }

        [Test]
        public void LostCount_RoundsUpAndNeverExceedsStock()
        {
            Assert.AreEqual(4, StashEventRoll.LostCount(5, StashEventKind.CityCleaning));   // ceil(3.5)
            Assert.AreEqual(2, StashEventRoll.LostCount(5, StashEventKind.ScavengedByPeers)); // ceil(1.5)
            Assert.AreEqual(5, StashEventRoll.LostCount(5, StashEventKind.PoliceRemoval));   // ceil(4.5)
            Assert.AreEqual(0, StashEventRoll.LostCount(0, StashEventKind.CityCleaning));
            Assert.AreEqual(0, StashEventRoll.LostCount(5, StashEventKind.None));
        }

        [Test]
        public void SelectLost_ReturnsThatManyDistinctInRangeIndices_Descending()
        {
            var lost = StashEventRoll.SelectLost(5, StashEventKind.CityCleaning, new SystemRng(123));
            Assert.AreEqual(4, lost.Count);

            var seen = new HashSet<int>();
            var previous = int.MaxValue;
            foreach (var index in lost)
            {
                Assert.IsTrue(index >= 0 && index < 5, "索引は在庫の範囲内。");
                Assert.IsTrue(seen.Add(index), "同じ点を二度失わない。");
                Assert.Less(index, previous, "降順で返す（抜いても索引がずれない）。");
                previous = index;
            }
        }
    }
}
