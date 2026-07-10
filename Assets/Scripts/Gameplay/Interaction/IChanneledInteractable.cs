namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// 「時間のかかる調べる」。押した瞬間に終わらず、一定時間かけて完了する調べもの。
    /// ゴミ箱漁りがこれにあたる。漁りには探索時間が要る（第十節）。
    ///
    /// この「完了までの間」は、後の段階が積み重なる土台でもある。警官はこの間に警告し（第五節）、
    /// コツ `手を止めない` はこの間の中断規則を書き換える（Phase 5）。したがってチャネルは
    /// 一級の概念として持ち、途中で歩き出せば中断し、立ち止まって待てば完遂する。
    /// </summary>
    public interface IChanneledInteractable : IInteractable
    {
        /// <summary>完了までにかかる秒数。0 以下なら即時（<see cref="IInteractable.Interact"/> と同じ）。</summary>
        float ChannelSeconds(PlayerContext player);

        /// <summary>
        /// 完了時に呼ばれる。<see cref="IInteractable.Interact"/> の代わりであり、
        /// 何が起きたかを世界の言葉で返す（第十四節）。null なら通知しない。
        /// </summary>
        string CompleteChannel(PlayerContext player);

        /// <summary>途中で中断されたときの後始末。資源を消費してはならない。</summary>
        void CancelChannel(PlayerContext player);
    }
}
