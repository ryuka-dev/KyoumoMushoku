using KyoumoMushoku.Core.Foraging;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 叩いた手応えの読み（`Docs/切片の品目.md` 第三節）の検証。SAN が下がると合図が消えるが、偽の合図は出ない。
    /// </summary>
    public sealed class HitTellTests
    {
        [TestCase(100f, HitTell.Detailed)]
        [TestCase(70f, HitTell.Detailed)]
        [TestCase(69.9f, HitTell.SomethingGood)]
        [TestCase(50f, HitTell.SomethingGood)]
        [TestCase(49.9f, HitTell.WeightOnly)]
        [TestCase(20f, HitTell.WeightOnly)]
        [TestCase(19.9f, HitTell.Nothing)]
        [TestCase(0f, HitTell.Nothing)]
        public void Read_FollowsSanityBands(float sanity, HitTell expected)
        {
            Assert.AreEqual(expected, HitTellSight.Read(sanity));
        }

        [Test]
        public void Detailed_TellsRareFromJackpot()
        {
            Assert.AreEqual(HitGlint.Rare, HitTellSight.Glint(HitTell.Detailed, LootTier.Rare));
            Assert.AreEqual(HitGlint.Jackpot, HitTellSight.Glint(HitTell.Detailed, LootTier.Jackpot));
        }

        [Test]
        public void SomethingGood_DoesNotTellRareFromJackpot()
        {
            Assert.AreEqual(HitGlint.Plain, HitTellSight.Glint(HitTell.SomethingGood, LootTier.Rare));
            Assert.AreEqual(HitGlint.Plain, HitTellSight.Glint(HitTell.SomethingGood, LootTier.Jackpot));
        }

        [TestCase(HitTell.WeightOnly)]
        [TestCase(HitTell.Nothing)]
        public void LowSanity_HidesGlint(HitTell tell)
        {
            Assert.AreEqual(HitGlint.None, HitTellSight.Glint(tell, LootTier.Jackpot));
        }

        [Test]
        public void NoRareInside_NeverGlints_AtAnySanity()
        {
            foreach (HitTell tell in System.Enum.GetValues(typeof(HitTell)))
            {
                Assert.AreEqual(HitGlint.None, HitTellSight.Glint(tell, LootTier.Uncommon), "珍しい物が無いのに光れば嘘になる。");
                Assert.AreEqual(HitGlint.None, HitTellSight.Glint(tell, LootTier.Common));
            }
        }

        [Test]
        public void WeightIsFeltUnlessBroken()
        {
            Assert.IsTrue(HitTellSight.FeelsWeight(HitTell.WeightOnly));
            Assert.IsFalse(HitTellSight.FeelsWeight(HitTell.Nothing));
        }
    }
}
