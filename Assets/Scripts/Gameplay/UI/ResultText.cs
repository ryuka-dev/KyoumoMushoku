namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 垂直スライスの結算画面（4日目の朝）のプレイヤー向け文字列を集約する。
    /// 語法はメソッド本体に閉じ込め、将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class ResultText
    {
        public static string Header => "― ４日目の朝 ―";
        public static string Survived => "3日間、生き延びた。";

        public static string GoalsHeading => "めあて";

        public static string WalletLine(int yen) => $"所持金　{yen}円";
        public static string KnacksLine(int count) => $"覚えたコツ　{count}つ";

        /// <summary>終わりであって終わりではない。スライスの外へは開いたまま送り出す（第八節・オープン型）。</summary>
        public static string Closing => "今日も、無職。";
        public static string ContinueHint => "（何かキーで続ける）";
    }
}
