using KyoumoMushoku.Core.Police;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 生活ゾーンの警戒度への「保管庫由来」の常時入力（第十二節）。段ボールを背負って歩くことと、
    /// 貯め込みすぎていることが、毎秒どれだけ警戒度を上げるかを純関数として閉じる。
    ///
    /// 「貯め込んだ財産は安全ではない」という中核の緊張を、そのまま読み取り可能な数値にする。
    /// 上げた警戒度は保管庫イベントの発生確率に効く（<see cref="StashEventRoll"/>）。
    /// </summary>
    public static class StashPressure
    {
        /// <summary>段ボールを背負って歩いている間、毎秒上がる分。担いでいないなら 0。</summary>
        public static float CarryRaisePerSecond(bool carrying) =>
            carrying ? ZoneAlertTuning.CarryResidentialRatePerSecond : 0f;

        /// <summary>1つの保管庫の貯め込みが毎秒上げる分。閾値を超えたマスにだけかかる。</summary>
        public static float HoardRaisePerSecond(int usedSlots)
        {
            var over = usedSlots - ZoneAlertTuning.HoardThresholdSlots;
            return over > 0 ? over * ZoneAlertTuning.HoardResidentialRatePerSecondPerSlot : 0f;
        }
    }
}
