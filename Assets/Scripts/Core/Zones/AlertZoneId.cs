namespace KyoumoMushoku.Core.Zones
{
    /// <summary>
    /// 警戒ゾーン。区切りは地形ではなく「誰が見ているか」で決まる。
    /// 3つのゾーンはいずれも警戒度を持ち、異なるのは
    /// その警戒度が何を駆動するかと、減衰の速さだけである。
    /// </summary>
    public enum AlertZoneId
    {
        /// <summary>どのゾーンにも属さない。</summary>
        None = 0,

        /// <summary>静穏：公園・公衆トイレ・病院。無料の就寝場所の撤去（敵対的建築）を駆動する。</summary>
        Quiet = 1,

        /// <summary>生活：路地裏・地下通路。保管庫イベントの発生確率を駆動する。</summary>
        Residential = 2,

        /// <summary>商業：コンビニ前・大通り・商店街入口。警察の段階進行の速さを駆動する。</summary>
        Commercial = 3,
    }
}
