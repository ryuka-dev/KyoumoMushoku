namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// ゴミ箱漁りの玩家向け文字列を集約する（第二・十三節）。インタラクトの説明、あたりの見分け方の覗き見、
    /// 漁った結果の通知をここに閉じ込める。食品の状態（新鮮／傷み／腐敗）はここでは言わない――
    /// 読めるかは SAN の問題であり、食品カードの `??` で読む（第三節）。
    ///
    /// 語法はメソッド本体に閉じ込め、将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class ForageText
    {
        // ── インタラクトの説明 ───────────────────────────────────
        public static string Depleted => "ゴミ箱（もう漁るものはない）";
        public static string BagFull => "ゴミ箱（カバンが一杯だ）";
        public static string Rummage => "ゴミ箱を漁る";

        // ── あたりの見分け方の覗き見（`??` の脇には必ず理由を添える・第三節） ──
        public static string PeekUnreadable => "（??・よく見えない）";
        public static string PeekEmpty => "（空っぽのようだ）";
        public static string PeekHit => "（当たりのようだ）";
        public static string PeekNamed(string name) => $"（{name}が見える）";

        // ── 漁った結果 ───────────────────────────────────────────
        public static string FoundEmpty => "空っぽだった。";
        public static string CannotCarry(string name) => $"{name}を見つけたが、もう担げない。";
        public static string CannotFit(string name) => $"{name}を見つけたが、カバンに入りきらない。";
        public static string Found(string name) => $"{name}が出た。";
    }
}
