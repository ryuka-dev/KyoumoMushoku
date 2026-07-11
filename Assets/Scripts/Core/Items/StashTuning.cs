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

        /// <summary>種別ごとの既定容量。未知の種別は最小限（1）にとどめる。</summary>
        public static int CapacityFor(StashKind kind) => kind switch
        {
            StashKind.CardboardBox => CardboardBoxCapacity,
            _ => 1,
        };
    }
}
