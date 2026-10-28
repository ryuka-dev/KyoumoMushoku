using KyoumoMushoku.Core.Police;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 警官の目に気づく力（SAN＝都市を読む力）の検証。SAN が下がると、まず背後の目に気づけなくなり、崩壊すると目の前の目にも気づけない。
    /// </summary>
    public sealed class PoliceAwarenessTests
    {
        [TestCase(100f, PoliceAwareness.Full)]
        [TestCase(50f, PoliceAwareness.Full)]
        [TestCase(49.9f, PoliceAwareness.InFrontOnly)]
        [TestCase(20f, PoliceAwareness.InFrontOnly)]
        [TestCase(19.9f, PoliceAwareness.None)]
        [TestCase(0f, PoliceAwareness.None)]
        public void Read_FollowsSanityBands(float sanity, PoliceAwareness expected)
        {
            Assert.AreEqual(expected, PoliceAwarenessSight.Read(sanity));
        }

        [Test]
        public void Full_ShowsEverywhere()
        {
            Assert.IsTrue(PoliceAwarenessSight.Shows(PoliceAwareness.Full, onScreen: true));
            Assert.IsTrue(PoliceAwarenessSight.Shows(PoliceAwareness.Full, onScreen: false));
        }

        [Test]
        public void InFrontOnly_ShowsOnlyOnScreen()
        {
            Assert.IsTrue(PoliceAwarenessSight.Shows(PoliceAwareness.InFrontOnly, onScreen: true));
            Assert.IsFalse(PoliceAwarenessSight.Shows(PoliceAwareness.InFrontOnly, onScreen: false));
        }

        [Test]
        public void None_ShowsNothing()
        {
            Assert.IsFalse(PoliceAwarenessSight.Shows(PoliceAwareness.None, onScreen: true));
            Assert.IsFalse(PoliceAwarenessSight.Shows(PoliceAwareness.None, onScreen: false));
        }
    }
}
