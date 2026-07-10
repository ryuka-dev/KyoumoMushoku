namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// SAN の帯。第三節の表と1対1で対応する。
    /// SAN は3本目の体力ゲージではなく、プレイヤーと世界のあいだにある解像度そのものである。
    /// 画面の色・プレイヤーの状態・情報の精度は、いずれもこの一本の線から導かれる。
    /// </summary>
    public enum SanityTier
    {
        /// <summary>精神崩壊（20 未満）。ほぼ完全なモノクロ。情報が `??` になる。</summary>
        Broken = 0,

        /// <summary>20〜49。明らかに色が抜ける。傷みと腐敗の区別がつかない。</summary>
        Dulled = 1,

        /// <summary>50〜69。わずかに色が抜ける。食品の状態が正確に分かる。</summary>
        Normal = 2,

        /// <summary>上機嫌（70 以上）。満色。NPC が話しかけてくる。</summary>
        Elated = 3,
    }
}
