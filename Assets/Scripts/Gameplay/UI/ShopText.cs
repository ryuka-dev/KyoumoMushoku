namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// コンビニ（購入・売却・バイト）の玩家向け文字列を1か所に集約する（第九・十三節）。
    ///
    /// 表示名は識別子とは別概念であり、語法（助詞・語順）は各メソッドの中に閉じ込める。
    /// 呼び出し側は文字列を組み立てず、意味でメソッドを呼ぶ。将来の多言語化は、メソッド本体を
    /// テーブル参照に差し替えるだけで済み、呼び出し側もゲームプレイのコードも一切変えずに済む。
    /// メソッド名がそのまま将来の参照キーになるため、意味で安定的に名づける。
    ///
    /// 診断ログ・Inspector のツールチップ・データ資産の表示名はここに含めない（玩家向けではない）。
    /// </summary>
    public static class ShopText
    {
        /// <summary>店の看板／インタラクトの見出し。</summary>
        public static string StoreName => "コンビニ";

        // ── 店主の台詞（購入） ───────────────────────────────────
        public static string BoughtThanks(string itemName) =>
            string.IsNullOrEmpty(itemName) ? "まいど。" : $"{itemName}、まいど。";
        public static string CannotAfford => "金が足りないよ。";
        public static string InventoryFull => "そんなに持てないだろ。";
        public static string AlreadyOwned => "もう持ってるだろ。";
        public static string NotSold => "それは置いてないよ。";

        // ── 店主の台詞（売却） ───────────────────────────────────
        public static string SalvageSold(int count, int paidYen, bool capReached)
        {
            var line = $"{count}点で{paidYen}円だ。";
            return capReached ? line + "…今日はこれで勘弁してくれ。" : line;
        }
        public static string BuybackCapReached => "今日はもう買い取れないよ。";
        public static string NothingToSell => "売る物がないな。";

        // ── バイトの精算 ─────────────────────────────────────────
        public static string JobDone(int paidYen) =>
            paidYen > 0 ? $"お疲れさん。{paidYen}円だ。" : "今日はもう帰りな。";

        // ── メニュー ─────────────────────────────────────────────
        public static string MenuHeader => "＝ コンビニ ＝　　［E／Esc で出る］";
        public static string MenuBuyHeading => "＜買う＞";
        public static string MenuOfferLine(int index, string name, string tail) => $"  {index}. {name}　{tail}";
        public static string OfferOwned => "購入済み";
        public static string OfferPrice(int yen) => $"{yen}円";
        public static string OfferPriceUnreadable => "??円";
        public static string PriceUnreadableNote => "　　「値札がぼやけて読めない」";
        public static string MenuSellHeading(int remainingYen) => $"＜売る＞　S：換金廃品を売る（本日あと {remainingYen}円）";
        public static string MenuWorkHeading => "＜はたらく＞　W：レジ打ち（気分と空腹を削る）";

        // ── バイト（レジ打ちのタイミングバー） ───────────────────
        public static string WorkHeader(int round, int totalRounds) => $"＝ レジ打ち ＝　ラウンド {round}/{totalRounds}";
        public static string WorkControls => "Space：打つ　　Esc：やめる";
        public static string WorkHits(int hits) => $"命中 {hits}";
    }
}
