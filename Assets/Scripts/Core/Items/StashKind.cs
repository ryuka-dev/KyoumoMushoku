namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫の種別（第十二節）。種別が違えば容量・安全性・場所代が違う。
    /// 初版は前期（路地裏の段ボール箱）と中期（安宿のコインロッカー）の2段階（仮住まいはスコープ外）。
    ///
    /// 種別を <see cref="StashState"/> に載せておくのは、後で種別を増やしたときに
    /// セーブ形式を作り直さずに済ませるためである（既存の箱はそのまま読める）。
    /// </summary>
    public enum StashKind
    {
        /// <summary>路地裏の段ボール箱。無料で開けられる・小・低安全。場所代を払えば安全性が上がる（第十二節）。</summary>
        CardboardBox = 0,

        /// <summary>安宿のコインロッカー。商業ゾーン・中容量・高安全（使用料を払っている限り）。住処から遠い（第十二節）。</summary>
        CoinLocker = 1,
    }
}
