namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 捨てるパネル（ゴミ箱・<see cref="DiscardPanel"/>）のプレイヤー向け文字列を集約する。
    /// 語法はメソッド本体に閉じ込め、将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class DiscardText
    {
        // ── インタラクトの促し（InteractionPrompt が ［G］ を添える） ──
        public static string Prompt => "持ち物を捨てる";

        // ── パネル ───────────────────────────────────────────────
        public static string PanelHeader => "ゴミ箱に捨てる";
        public static string BagHeading(int usedSlots, int capacity) => $"カバン　{usedSlots}/{capacity}マス";
        public static string ItemLine(int index, string name) => $"　{index}）{name}";
        public static string BagEmpty => "　（カバンは空だ）";
        public static string CarryLine(string name) => $"　0）{name}を下ろして捨てる（背負っている）";
        public static string CloseHint => "数字キーで捨てる　E で閉じる";

        // ── 結果 ─────────────────────────────────────────────────
        public static string Discarded(string name) => $"{name}を捨てた。";
    }
}
