namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 時間によって閉まる使う物（昼だけ開く店）。閉まっている間は触れる物ではないので、縁も照準の輪も案内も消える
    /// （`GameDesign.md` 第十七節 規則 1）。閉まっていることは、世界の側（下りたシャッター）が見せる。
    /// </summary>
    public interface IProtoClosable
    {
        bool IsOpenNow { get; }
    }
}
