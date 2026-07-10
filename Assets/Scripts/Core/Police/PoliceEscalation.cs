using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 注目度（1人の警官がプレイヤーに向けている関心）とその段階（第五節）。
    ///
    /// 注目度は警官ごとの一過性の値であり、視界を外れれば減衰して消える。永続する警戒度
    /// （<see cref="ZoneAlertLevels"/>）とは別概念であって、同じ数値にまとめてはならない。
    /// 両者は一方向にのみ結び付く。<b>商業ゾーンの警戒度が、そこでの段階進行を速める。</b>
    ///
    /// 数値はすべて叩き台であり、プレイテストで調整する。
    /// </summary>
    public static class PoliceEscalation
    {
        public const float MinSuspicion = 0f;
        public const float MaxSuspicion = 100f;

        public const float NoticingThreshold = 25f;
        public const float WarningThreshold = 60f;
        public const float PursuingThreshold = 100f;

        /// <summary>視界内でただ佇んでいるだけでも、長時間の滞在は注意を招く（第五節・第1段階）。</summary>
        public const float LoiterGainPerSecond = 4f;

        /// <summary>視界を外れれば冷めていく。立ち去ることが常に有効な応答である。</summary>
        public const float OutOfSightDecayPerSecond = 12f;

        public static PoliceStage StageFor(float suspicion)
        {
            if (suspicion >= PursuingThreshold)
            {
                return PoliceStage.Pursuing;
            }

            if (suspicion >= WarningThreshold)
            {
                return PoliceStage.Warning;
            }

            return suspicion >= NoticingThreshold ? PoliceStage.Noticing : PoliceStage.Unaware;
        }

        /// <summary>
        /// 一度始まった追跡は、注目度が完全に冷めるまで終わらない。
        ///
        /// これがないと、注目度が閾値をわずかに下回った瞬間に警官が追跡をやめ、境界の上で
        /// 追跡と警告を往復してしまう。「追われている」という状態は、逃げ切って初めて終わる。
        /// </summary>
        public static PoliceStage NextStage(PoliceStage current, float suspicion)
        {
            if (current != PoliceStage.Pursuing)
            {
                return StageFor(suspicion);
            }

            return suspicion <= MinSuspicion ? PoliceStage.Unaware : PoliceStage.Pursuing;
        }

        /// <summary>
        /// 段階進行の速さに掛かる倍率。商業ゾーンの警戒度だけがこれを駆動する（第五節・第十三節）。
        /// 警戒度 100 で 2 倍速。他のゾーンの警戒度は段階進行に影響しない。
        /// </summary>
        public static float SpeedMultiplier(AlertZoneId officerZone, float alertLevel)
        {
            if (officerZone != AlertZoneId.Commercial)
            {
                return 1f;
            }

            var normalized = ZoneAlertTuning.Clamp(alertLevel) / ZoneAlertTuning.MaxLevel;
            return 1f + normalized;
        }

        /// <summary>視界内で目立つ行為を続けている間の上昇。</summary>
        public static float Advance(float suspicion, float gainPerSecond, float speedMultiplier, float deltaSeconds)
        {
            if (deltaSeconds <= 0f || gainPerSecond <= 0f)
            {
                return Clamp(suspicion);
            }

            return Clamp(suspicion + gainPerSecond * speedMultiplier * deltaSeconds);
        }

        /// <summary>視界外にいる間の減衰。ここに警戒度の倍率は掛からない。冷めるのは誰にとっても同じ速さである。</summary>
        public static float Relax(float suspicion, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return Clamp(suspicion);
            }

            return Clamp(suspicion - OutOfSightDecayPerSecond * deltaSeconds);
        }

        public static float Clamp(float suspicion)
        {
            if (suspicion < MinSuspicion)
            {
                return MinSuspicion;
            }

            return suspicion > MaxSuspicion ? MaxSuspicion : suspicion;
        }
    }
}
