namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 照準を合わせて左クリックで使う物（店の台・水道・寝床）。<see cref="ProtoHands"/> が照準の先で探す。
    /// コライダーと同じ物体か、その親に付ける。
    /// </summary>
    public interface IProtoUsable
    {
        /// <summary>照準を合わせたときの案内。出さないなら null。</summary>
        string Prompt(ProtoHands hands);

        /// <summary>使う。返すのは結果の一言（世界の言葉で）。</summary>
        string Use(ProtoHands hands);
    }
}
