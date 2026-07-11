using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 先輩ホームレスの発話選択（第十四節）。事後説明は SAN を問わず最優先、噂話だけが SAN 70 以上を要する。
    /// </summary>
    public sealed class ElderRemarkTests
    {
        const int OverFull = ZoneAlertTuning.HoardThresholdSlots + 1;
        const int NotFull = ZoneAlertTuning.HoardThresholdSlots;

        [Test]
        public void Aftermath_TakesPriority_RegardlessOfForecastOrSanity()
        {
            // 起きたことの説明は、予告があっても SAN が低くても最優先で語られる。
            Assert.AreEqual(ElderRemarkKind.Aftermath,
                ElderRemark.Decide(StashEventKind.CityCleaning, StashEventKind.PoliceRemoval, OverFull, 0f));
        }

        [Test]
        public void Rumor_OnlyReachesElatedMinds()
        {
            Assert.AreEqual(ElderRemarkKind.Rumor,
                ElderRemark.Decide(StashEventKind.None, StashEventKind.CityCleaning, NotFull, 70f));

            // SAN 70 未満では噂話は届かない。貯め込みもしていなければ何も言わない。
            Assert.AreEqual(ElderRemarkKind.None,
                ElderRemark.Decide(StashEventKind.None, StashEventKind.CityCleaning, NotFull, 69.9f));
        }

        [Test]
        public void HoardNag_FillsInWhenThereIsNothingElseToSay()
        {
            // 予告があっても SAN が低くて届かないなら、代わりに膨らみすぎの小言に落ちる。
            Assert.AreEqual(ElderRemarkKind.HoardNag,
                ElderRemark.Decide(StashEventKind.None, StashEventKind.CityCleaning, OverFull, 10f));

            // 予告も無く、ただ膨らんでいるだけでも小言。
            Assert.AreEqual(ElderRemarkKind.HoardNag,
                ElderRemark.Decide(StashEventKind.None, StashEventKind.None, OverFull, 100f));
        }

        [Test]
        public void Silence_WhenThereIsNothingToRemarkOn()
        {
            Assert.AreEqual(ElderRemarkKind.None,
                ElderRemark.Decide(StashEventKind.None, StashEventKind.None, NotFull, 100f));
        }

        [Test]
        public void CanHearRumors_MatchesTheElatedThreshold()
        {
            Assert.IsTrue(SanityScale.CanHearRumors(SanityScale.ElatedThreshold));
            Assert.IsFalse(SanityScale.CanHearRumors(SanityScale.ElatedThreshold - 0.1f));
        }
    }
}
