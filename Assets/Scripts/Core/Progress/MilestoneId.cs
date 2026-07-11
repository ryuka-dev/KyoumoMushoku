namespace KyoumoMushoku.Core.Progress
{
    /// <summary>
    /// 垂直スライス（3ゲームデイ）の段階目標（第八・九節）。
    /// コツ（第六節）がルール変化であるのに対し、こちらは進行の記録であり、何のルールも変えない。
    /// </summary>
    public enum MilestoneId
    {
        /// <summary>3日間生存する。HP0 は搬送であって死ではないため、4日目の朝を迎えることが達成である。</summary>
        SurviveThreeDays = 0,

        /// <summary>初めて安宿に泊まる。</summary>
        FirstInnStay = 1,

        /// <summary>バックパックを購入する。3日目のマイルストーン（第十一節）。</summary>
        BuyBackpack = 2,
    }
}
