using System;
using KyoumoMushoku.Core.Randomness;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 静穏ゾーンの警戒度が駆動する唯一の事項：就寝時に叩き起こされる確率（第五節）。
    ///
    /// これにより、同じベンチで寝続けると警官に顔を覚えられ、無料のベッドが自分自身を腐食していく。
    /// バランス調整のための追加規則を必要とせずに、無料の就寝場所が支配的な選択肢になることを防げる。
    ///
    /// 数値は叩き台であり、プレイテストで調整する。
    /// </summary>
    public static class SleepDisturbance
    {
        /// <summary>警戒度が満杯でも、必ず起こされるわけではない。</summary>
        public const float MaxProbability = 0.8f;

        /// <summary>叩き起こされた夜は休めていない。回復量はこの倍率で目減りする。</summary>
        public const float WokenRecoveryScale = 0.5f;

        /// <summary>静穏ゾーンの警戒度に対して単調増加する。</summary>
        public static float Probability(float quietAlertLevel)
        {
            var normalized = ZoneAlertTuning.Clamp(quietAlertLevel) / ZoneAlertTuning.MaxLevel;
            return normalized * MaxProbability;
        }

        public static bool Roll(float quietAlertLevel, IRng rng)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            return rng.NextDouble() < Probability(quietAlertLevel);
        }
    }
}
