using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>第三節の表「一本の線が持つ三重の意味」を実装に固定する。</summary>
    public sealed class SanityScaleTests
    {
        [TestCase(100f, SanityTier.Elated)]
        [TestCase(70f, SanityTier.Elated)]
        [TestCase(69.9f, SanityTier.Normal)]
        [TestCase(50f, SanityTier.Normal)]
        [TestCase(49.9f, SanityTier.Dulled)]
        [TestCase(20f, SanityTier.Dulled)]
        [TestCase(19.9f, SanityTier.Broken)]
        [TestCase(0f, SanityTier.Broken)]
        public void TierOf_MatchesTheTable(float sanity, SanityTier expected)
        {
            Assert.AreEqual(expected, SanityScale.TierOf(sanity));
        }

        [Test]
        public void Saturation_IsFullAtSeventyAndAbove()
        {
            Assert.AreEqual(1f, SanityScale.Saturation(70f), 1e-5f);
            Assert.AreEqual(1f, SanityScale.Saturation(100f), 1e-5f);
        }

        [Test]
        public void Saturation_IsMonochromeAtZero()
        {
            Assert.AreEqual(0f, SanityScale.Saturation(0f), 1e-5f);
        }

        [Test]
        public void Saturation_BarelyMovesNearFiftyAndCollapsesBelowThirty()
        {
            // 50 付近ではほとんど気づかない。
            Assert.Greater(SanityScale.Saturation(50f), 0.8f);

            // 20 未満はほぼ完全なモノクロ。
            Assert.Less(SanityScale.Saturation(20f), 0.2f);
        }

        [Test]
        public void Saturation_StartsGentlyAndFallsSharply()
        {
            var nearFullColor = SanityScale.Saturation(70f) - SanityScale.Saturation(60f);
            var nearCollapse = SanityScale.Saturation(40f) - SanityScale.Saturation(30f);

            Assert.Less(nearFullColor, nearCollapse * 0.2f,
                "退色は緩やかに始まり、急に落ちる。同じ 10 の低下でも失う色の量が違う。");
        }

        [Test]
        public void Saturation_NeverIncreasesAsSanityFalls()
        {
            var previous = SanityScale.Saturation(100f);
            for (var sanity = 99f; sanity >= 0f; sanity -= 1f)
            {
                var current = SanityScale.Saturation(sanity);
                Assert.LessOrEqual(current, previous + 1e-6f);
                previous = current;
            }
        }

        [Test]
        public void Conversation_ClosesAtThirty_ButSurvivalNeverDoes()
        {
            Assert.IsTrue(SanityScale.CanRespondToConversation(30f));
            Assert.IsFalse(SanityScale.CanRespondToConversation(29.9f));
        }

        [Test]
        public void SleepRecovery_DropsAtBreakdownButKeepsAFloor()
        {
            Assert.AreEqual(12f, SanityScale.SleepRecovery(12f, SanityScale.BreakdownThreshold), 1e-5f);

            // 半減した 6 は下限 5 を上回るため、そのまま。
            Assert.AreEqual(6f, SanityScale.SleepRecovery(12f, 5f), 1e-5f);

            // 半減した 3 は下限 5 を下回るため、下限まで戻す。
            Assert.AreEqual(5f, SanityScale.SleepRecovery(6f, 5f), 1e-5f);
        }

        [Test]
        public void SleepRecovery_FloorNeverExceedsWhatTheSpotOffers()
        {
            // 元から 4 しか回復しない就寝場所を、下限が 5 に引き上げてはならない。
            Assert.AreEqual(4f, SanityScale.SleepRecovery(4f, 0f), 1e-5f);
            Assert.AreEqual(0f, SanityScale.SleepRecovery(0f, 0f), 1e-5f);
        }
    }
}
