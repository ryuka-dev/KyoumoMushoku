namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// プレイヤーが調べられる世界の物体。拾う・飲む・就寝・買う、いずれもこの口を実装する。
    ///
    /// 予告も事後説明も世界の中の情報として行う（第十四節）。したがって使えない理由は
    /// <see cref="Describe"/> がその場で言葉にし、「見えていない」と「作られていない」を
    /// プレイヤーが取り違えないようにする。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>いま実際に押して意味があるか。</summary>
        bool CanInteract(PlayerContext player);

        /// <summary>
        /// プロンプトに出す一行。使える時は行動（「水を飲む」）、使えない時は理由（「カバンが一杯だ」）。
        /// キー表記は UI が添えるため、ここには含めない。
        /// </summary>
        string Describe(PlayerContext player);

        void Interact(PlayerContext player);
    }
}
