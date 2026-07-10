namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 警察の段階（第五節）。警察は接触した瞬間に攻撃してくる敵ではなく、
    /// 段階的にエスカレートする追い出し機構である。
    ///
    /// 段階は注目度（<see cref="PoliceEscalation"/>）から導かれる派生値であって、
    /// 独立に保持される権威ではない。
    /// </summary>
    public enum PoliceStage
    {
        /// <summary>まだ気づかれていない。</summary>
        Unaware = 0,

        /// <summary>第1段階：注意。警察が注意を向け始める。</summary>
        Noticing = 1,

        /// <summary>第2段階：警告。近づいてきて、エリアから離れるよう要求する。</summary>
        Warning = 2,

        /// <summary>第3段階：追い出し。立ち去りを拒否したため、追跡が始まる。</summary>
        Pursuing = 3,
    }
}
