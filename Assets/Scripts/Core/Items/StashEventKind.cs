namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫を脅かすイベントの種別（第十二節）。いずれもランダム発生であり、警戒度は発生確率への
    /// 複数入力のうちの1つにすぎない（唯一の要因ではない）。
    ///
    /// 各イベントには「世界の中の物体」による保証チャネル（SAN を問わず必ず見える予告）が対応する。
    /// </summary>
    public enum StashEventKind
    {
        /// <summary>何も起きない。</summary>
        None = 0,

        /// <summary>市の清掃。主因は保管庫のあるゾーンの警戒度。保管物の大部分を失う。予告＝清掃予告の貼り紙。</summary>
        CityCleaning = 1,

        /// <summary>同業者に漁られる。主因は保管物の量。保管物の一部を失う。予告＝荒らされた足跡。</summary>
        ScavengedByPeers = 2,

        /// <summary>警察の撤去。主因は高い警戒度。保管物を失い、さらにゾーンの警戒度が上がる。予告＝下見する警官。</summary>
        PoliceRemoval = 3,
    }
}
