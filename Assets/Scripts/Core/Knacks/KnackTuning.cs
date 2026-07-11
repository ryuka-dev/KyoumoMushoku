namespace KyoumoMushoku.Core.Knacks
{
    /// <summary>
    /// コツの触発閾値と、コツが変化させる規則の数値（第六節）。
    /// 触発条件は具体的な行動に紐づくため、ゴミ箱の産出頻度（第十三節）と警察の警告頻度（第五節）は
    /// これらの閾値に合わせて調整する必要がある。数値はすべて叩き台であり、プレイテストで調整する。
    /// </summary>
    public static class KnackTuning
    {
        /// <summary>あたりの見分け方：ゴミ箱を漁った累積回数がこれに達すると習得する。</summary>
        public const int RummagesForSpotDuds = 10;

        /// <summary>手を止めない：漁り中に警告された累積回数がこれに達すると習得する。</summary>
        public const int ForageWarningsForSteadyHands = 3;

        /// <summary>路上の寝方：野外（無料の寝床）で就寝した累積回数がこれに達すると習得する。</summary>
        public const int OutdoorSleepsForStreetSleeper = 2;

        // 鉄の胃袋・通りすがりの顔は閾値1（「初めて」）であり、習得済みフラグがそのままカウンタを兼ねる。

        /// <summary>
        /// 鉄の胃袋：習得後、腐敗した食品の代償（HP・SAN）にかかる倍率。腐敗のダメージが半減する。
        /// </summary>
        public const float IronStomachRottenScale = 0.5f;

        /// <summary>
        /// 路上の寝方：習得後、野外就寝に上乗せされる回復量（HP・SAN）。ルール変化に付随する小さな数値上昇。
        /// 無料のベンチを実際に持続可能にするための下支えであり、支配的な選択肢にはしない。
        /// </summary>
        public const float StreetSleeperRecoveryBonus = 6f;

        /// <summary>
        /// 路上の寝方：習得後、野外の無料就寝が静穏ゾーンに加える警戒度にかかる倍率。
        /// 目立たない寝方を覚えて苦情が減る＝ベンチが撤去閾値に達しにくくなる（撤去モデル・第五節）。
        /// 「叩き起こされにくくなる」の撤去モデルにおける相当物である。
        /// </summary>
        public const float StreetSleeperFreeSleepScale = 0.5f;
    }
}
