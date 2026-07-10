namespace KyoumoMushoku.Core.DayCycle
{
    /// <summary>
    /// 1ゲームデイを構成する3つの時間帯。
    /// </summary>
    public enum DayPhase
    {
        /// <summary>昼。</summary>
        Day,

        /// <summary>夕方。警察の警戒判定の閾値が短縮される。</summary>
        Dusk,

        /// <summary>夜。資源テーブルが夜間版に切り替わる。時間の上限は持たない。</summary>
        Night,
    }
}
