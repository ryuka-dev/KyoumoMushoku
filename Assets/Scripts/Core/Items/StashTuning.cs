namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫の数値（叩き台）。プレイテストで調整する（第十五節）。
    /// </summary>
    public static class StashTuning
    {
        /// <summary>
        /// 段ボール箱の容量（マス数）。カバンの初期容量（6）より広く、貯め込む価値がある一方、
        /// 命をつなぐ食料と値の張る廃品が同じ空間を奪い合う設計意図は保つ（第十一節）。叩き台。
        /// </summary>
        public const int CardboardBoxCapacity = 12;

        /// <summary>
        /// コインロッカーの容量（マス数）。段ボール箱（小＝12）より広い中容量（第十二節）。
        /// 段階が上がるほど貯め込める＝「都市に居場所を持つ」報酬になる。叩き台。
        /// </summary>
        public const int CoinLockerCapacity = 18;

        /// <summary>種別ごとの既定容量。未知の種別は最小限（1）にとどめる。</summary>
        public static int CapacityFor(StashKind kind) => kind switch
        {
            StashKind.CardboardBox => CardboardBoxCapacity,
            StashKind.CoinLocker => CoinLockerCapacity,
            _ => 1,
        };

        /// <summary>段ボール箱の場所代（1日・円）。第十二節・価格表（第十一節）の 300 円。叩き台。</summary>
        public const int CardboardBoxRentYen = 300;

        /// <summary>
        /// コインロッカーの使用料（1日・円）。段ボール箱より高いが、その分ずっと安全（第十二節）。
        /// 「継続的に有料」で高安全を買う中期の器。安宿1泊（1500）よりは安い。叩き台。
        /// </summary>
        public const int CoinLockerRentYen = 500;

        /// <summary>
        /// 種別ごとの場所代・使用料（1日・円）。0 なら料金を取らない種別。払うとその日の安全性が上がり、
        /// 保管庫イベントの発生確率が下がる（<see cref="StashSafety"/>）。未知の種別は 0（取らない）。
        /// </summary>
        public static int RentCostFor(StashKind kind) => kind switch
        {
            StashKind.CardboardBox => CardboardBoxRentYen,
            StashKind.CoinLocker => CoinLockerRentYen,
            _ => 0,
        };
    }
}
