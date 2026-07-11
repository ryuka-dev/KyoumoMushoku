namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 世界の小物（就寝場所・階段・地面の拾い物）の玩家向け文字列を集約する（第五・十三節）。
    /// 就寝場所の呼び名そのもの（「ベンチで寝る」等）は設置場所ごとに設定される data であり、ここには含めない。
    /// ここに集めるのは、その呼び名に添える接尾辞や、貼り紙・拾う動作といったコード側の語である。
    ///
    /// 語法はメソッド本体に閉じ込め、将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class WorldText
    {
        // ── 就寝場所の説明の接尾辞（呼び名 label に添える） ───────
        public static string SleepRemoved(string label) => $"{label}（撤去された）";
        public static string SleepCannotAfford(string label) => $"{label}（所持金が足りない）";
        public static string SleepCost(string label, int costYen) => $"{label}（{costYen}円）";
        public static string SleepWarned(string label) => $"{label}（苦情の貼り紙）";

        // ── 就寝場所の貼り紙（敵対的建築・予告と事後説明／第五・十四節） ──
        public static string SleepNoticeRemoved => "撤去済み（苦情多数のため）";
        public static string SleepNoticeWarned => "苦情の貼り紙：はやく出ていけ";

        // ── 階段（交互門・第五節） ───────────────────────────────
        public static string StairsDown => "階段を降りる";
        public static string StairsUp => "階段を昇る";

        // ── 地面の拾い物 ─────────────────────────────────────────
        public static string Pick(string name) => $"{name}を拾う";
        public static string PickBagFull(string name) => $"{name}（カバンが一杯だ）";
    }
}
