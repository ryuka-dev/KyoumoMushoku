using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 警戒度の増減規則（第五節）。3つのゾーンはいずれも警戒度を持ち、
    /// 異なるのは「その警戒度が何を駆動するか」と「減衰の速さ」の2点だけである。
    ///
    /// 数値はすべて叩き台であり、プレイテストで調整する。
    /// </summary>
    public sealed class ZoneAlertTuning
    {
        public const float MinLevel = 0f;
        public const float MaxLevel = 100f;

        /// <summary>捕まったゾーンの警戒度は上昇する（第五節・第3段階）。</summary>
        public const float CaptureRaise = 25f;

        /// <summary>
        /// 無料の就寝場所で寝ると、そのゾーンに顔を覚えられる。無料のベッドが自分自身を腐食していく（第五節）。
        /// 金を払って泊まる安宿では誰も気に留めない。
        ///
        /// これが3つの就寝場所を実質的に差別化する（第十三節）。ベンチに通い詰めれば起こされやすくなり、
        /// 地下通路に通い詰めれば保管庫が危うくなる（後者の帰結は Phase 5）。
        /// </summary>
        public const float FreeSleepRaise = 20f;

        /// <summary>
        /// 商業ゾーンで追われて生活ゾーンへ逃げ帰る行為は、生活ゾーンの警戒度への最大の入力である（第十三節）。
        /// 自分の住処の近くで目立つと、自分の住処を失う。
        /// </summary>
        public const float FleeIntoResidentialRaise = 20f;

        public static ZoneAlertTuning Default { get; } = new ZoneAlertTuning();

        /// <summary>
        /// 時間経過による毎秒の減衰。静穏は速く、商業は遅い（第五節）。
        ///
        /// 1日の昼と夕方は 240〜480 秒（<c>DayScheduleAsset</c>）。この尺度で選んである。
        /// 速くしすぎると、一晩で覚えられた顔がその日のうちに忘れられ、警戒度が何も蓄積しない。
        /// </summary>
        public float DecayPerSecond(AlertZoneId zone) => zone switch
        {
            AlertZoneId.Quiet => 0.015f,
            AlertZoneId.Residential => 0.01f,
            AlertZoneId.Commercial => 0.005f,
            _ => 0f,
        };

        /// <summary>就寝（日付の切り替わり）による減衰（第五節）。</summary>
        public float SleepDecay(AlertZoneId zone) => zone switch
        {
            AlertZoneId.Quiet => 4f,
            AlertZoneId.Residential => 3f,
            AlertZoneId.Commercial => 2f,
            _ => 0f,
        };

        public static float Clamp(float level)
        {
            if (level < MinLevel)
            {
                return MinLevel;
            }

            return level > MaxLevel ? MaxLevel : level;
        }
    }
}
