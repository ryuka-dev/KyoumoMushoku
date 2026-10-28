namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// いまは触れる物ではない時がある使う物：時間によって閉まる（昼だけ開く店）、または使い切った（今日はもう探った自販機の下）。
    /// その間は縁も照準の輪も案内も消える（`GameDesign.md` 第十七節 規則 1）。閉まっていることは世界の側（下りたシャッター）が見せる。
    /// </summary>
    public interface IProtoClosable
    {
        bool IsOpenNow { get; }
    }
}
