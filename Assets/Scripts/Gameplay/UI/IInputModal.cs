namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// ホットキーを一時的に占有する画面（店パネルなど）。開いている間は、数字キーによる飲食のような
    /// 常設のホットキーを黙らせ、キーの取り合いを避ける。誰が入力を握っているかを1つの所有者に閉じる。
    /// </summary>
    public interface IInputModal
    {
        bool IsOpen { get; }
    }
}
