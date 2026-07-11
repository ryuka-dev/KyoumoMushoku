using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Police;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 生活ゾーンの警戒度への保管庫由来の常時入力（第十二節）。背負い歩きは担いでいる間だけ、
    /// 貯め込みは閾値を超えたマスにだけかかる。
    /// </summary>
    public sealed class StashPressureTests
    {
        [Test]
        public void CarryRaise_OnlyAppliesWhileCarrying()
        {
            Assert.AreEqual(ZoneAlertTuning.CarryResidentialRatePerSecond,
                StashPressure.CarryRaisePerSecond(true), 1e-5f);
            Assert.AreEqual(0f, StashPressure.CarryRaisePerSecond(false), 1e-5f);
        }

        [Test]
        public void HoardRaise_AppliesOnlyToSlotsAboveTheThreshold()
        {
            Assert.AreEqual(0f, StashPressure.HoardRaisePerSecond(0), 1e-5f);
            Assert.AreEqual(0f, StashPressure.HoardRaisePerSecond(ZoneAlertTuning.HoardThresholdSlots), 1e-5f,
                "閾値ちょうどでは目立たない。");

            var used = ZoneAlertTuning.HoardThresholdSlots + 8;
            Assert.AreEqual(8 * ZoneAlertTuning.HoardResidentialRatePerSecondPerSlot,
                StashPressure.HoardRaisePerSecond(used), 1e-5f, "超過分にだけかかる。");
        }
    }
}
