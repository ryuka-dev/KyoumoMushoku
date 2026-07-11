namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫の種別（第十二節）。種別が違えば容量・安全性・場所代の有無が違う。
    /// 初版は路地裏の段ボール箱1種のみ。安宿のコインロッカーは 5b-3 で加える。
    ///
    /// 種別を <see cref="StashState"/> に載せておくのは、後で種別を増やしたときに
    /// セーブ形式を作り直さずに済ませるためである（既存の箱はそのまま読める）。
    /// </summary>
    public enum StashKind
    {
        /// <summary>路地裏の段ボール箱。無料・小・低安全（第十二節）。</summary>
        CardboardBox = 0,
    }
}
