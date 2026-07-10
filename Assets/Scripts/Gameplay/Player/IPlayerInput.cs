namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// プレイヤー入力の読み取り口。具体的な入力ライブラリはこの境界の内側に留める。
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>-1（左）から 1（右）。</summary>
        float Horizontal { get; }

        bool RunHeld { get; }

        /// <summary>このフレームでインタラクト（拾う・飲む・調べる・就寝）が押されたか。</summary>
        bool InteractPressed { get; }
    }
}
