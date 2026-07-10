namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// ゴミ箱3種（第十三節）。中身の傾向が異なるだけでなく、置かれる場所の警戒ゾーンも異なる。
    /// ここでは「どの産出テーブルを引くか」という識別子としてのみ用いる。
    /// </summary>
    public enum TrashCanKind
    {
        /// <summary>A：公園。アルミ缶・空き瓶が中心。食品は少ない。</summary>
        Park = 0,

        /// <summary>B：コンビニ前。夜に弁当・パンが出る。状態が良い確率が高いが、危険。</summary>
        ConvenienceStore = 1,

        /// <summary>C：路地裏の大型ゴミ箱。壊れた傘などの廃品。食品は状態が悪い。</summary>
        BackAlley = 2,
    }
}
