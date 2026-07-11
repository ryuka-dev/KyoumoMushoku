namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// SAN の一本の線が持つ三重の意味（画面の色・状態・情報の精度）を、ひとつの場所で定義する。
    /// SAN が関わる仕様を追加するときは、必ずここに閾値を集約すること。
    /// 個々の数値は叩き台であり、プレイテストでの調整を前提とする。
    /// </summary>
    public static class SanityScale
    {
        public const float Max = 100f;

        /// <summary>上機嫌。満色になり、NPC が向こうから話しかけてくる。噂話が届く。</summary>
        public const float ElatedThreshold = 70f;

        /// <summary>これを下回ると、食品の傷みと腐敗の区別がつかなくなる。</summary>
        public const float ClearSightThreshold = 50f;

        /// <summary>これを下回ると、搭話に応じられない。失われるのは「応じる」ことであって「聞こえる」ことではない。</summary>
        public const float ConversationThreshold = 30f;

        /// <summary>精神崩壊。情報が `??` になり、社会的行動が封鎖される。生き延びる手段は決して塞がない。</summary>
        public const float BreakdownThreshold = 20f;

        /// <summary>精神崩壊時、睡眠による SAN 回復量にかかる倍率。</summary>
        public const float BrokenSleepRecoveryMultiplier = 0.5f;

        /// <summary>精神崩壊時、バイトの効率（報酬倍率）。暴落するが、0 にはしない。「割に合わないが、まだできる」（第三節）。</summary>
        public const float BrokenJobEfficiency = 0.3f;

        /// <summary>睡眠による SAN 回復量の下限。回復不能な悪循環に陥らせないために設ける。</summary>
        public const float MinimumSleepRecovery = 5f;

        public static SanityTier TierOf(float sanity)
        {
            if (sanity >= ElatedThreshold)
            {
                return SanityTier.Elated;
            }

            if (sanity >= ClearSightThreshold)
            {
                return SanityTier.Normal;
            }

            return sanity >= BreakdownThreshold ? SanityTier.Dulled : SanityTier.Broken;
        }

        /// <summary>
        /// ワールドの描画にかける彩度（0＝完全なモノクロ、1＝満色）。
        /// 70 で満色、0 で完全なモノクロ。線形ではなく緩やかに始まり急に落ちる。
        /// 最も急なのは 35 付近であり、日常は普通に見え、危機だけが一目で分かる。
        ///
        /// UI の文字には決して適用しないこと。情報の欠落は `??` という明示的な形でのみ発生させる。
        /// </summary>
        public static float Saturation(float sanity)
        {
            var t = Clamp01(sanity / ElatedThreshold);

            // smootherstep: 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>低 SAN で失われるのは「他者と関わる行動」だけである。確率ではなく閾値で決める。</summary>
        public static bool CanRespondToConversation(float sanity) => sanity >= ConversationThreshold;

        /// <summary>
        /// 噂話が耳に届くか（第十二・十四節）。SAN 70 以上でのみ、先輩ホームレスの前倒しの予告・対抗手段の助言が聞こえる。
        /// 低 SAN で失われるのは「応じる」ことであって「聞こえる」ことではない――事後説明や貼り紙は SAN を問わず届く。
        /// </summary>
        public static bool CanHearRumors(float sanity) => sanity >= ElatedThreshold;

        /// <summary>
        /// 商品の価格が読めるか。精神崩壊すると値札がぼやける（第三節：商品の価格 ??）。
        /// 読めなくても買う行為そのものは決して封鎖しない。欠落は用いるが、操作は奪わない。
        /// </summary>
        public static bool CanReadPrices(float sanity) => sanity >= BreakdownThreshold;

        /// <summary>
        /// バイトの効率（報酬倍率）。SAN が崩壊帯（20 未満）に落ちると暴落する（第三節）。
        /// 禁止ではなく暴落なのは、バイトが唯一の安定収入であり、進行不能に陥らせないためである。
        /// </summary>
        public static float JobEfficiency(float sanity) => sanity >= BreakdownThreshold ? 1f : BrokenJobEfficiency;

        /// <summary>
        /// 睡眠による SAN の回復量。精神崩壊時は下がるが、下限を割らない。
        /// 下限が元の回復量を上回ることはない（元から少ない就寝場所を有利にしないため）。
        /// </summary>
        public static float SleepRecovery(float baseAmount, float sanity)
        {
            if (baseAmount <= 0f)
            {
                return 0f;
            }

            if (sanity >= BreakdownThreshold)
            {
                return baseAmount;
            }

            var reduced = baseAmount * BrokenSleepRecoveryMultiplier;
            var floor = baseAmount < MinimumSleepRecovery ? baseAmount : MinimumSleepRecovery;
            return reduced > floor ? reduced : floor;
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
