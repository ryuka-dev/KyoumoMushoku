namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// HUD まわりのプレイヤー向け文字列を集約する（第三・十四節）。状態表示・カバン一覧・行動トースト・
    /// インタラクトの促しなど、画面に常時／短時間出る UI の語をここに閉じ込める。
    ///
    /// 枚挙型の表示名（時間帯・ゾーン・気分・コツ）は <see cref="GameTextLabels"/> が、
    /// 食品カードは <see cref="FoodCardText"/> が持つ。ここはそれらを組み立てる外枠を受け持つ。
    /// 語法はメソッド本体に閉じ込め、将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class HudText
    {
        /// <summary>データがまだ無いときの空欄表示。</summary>
        public static string Unknown => "―";

        // ── 状態表示（VitalsHud） ────────────────────────────────
        public static string Status(int day, string phase, int yen, string mood, string zone) =>
            $"{day}日目　{phase}　　所持金 {yen}円\n気分：{mood}　　現在地：{zone}";

        // ── 状態ゲージのラベル（VitalsHud・各ゲージが何かを左に添える） ──
        // 食品カード（FoodCardText）と同じ語を使い、同じものを別名で呼ばない。
        public static string HpLabel => "HP";
        public static string ThirstLabel => "喉";
        public static string HungerLabel => "空腹";
        public static string SanityLabel => "気分";

        // ── カバン一覧（InventoryView） ──────────────────────────
        public static string BagHeader(int usedSlots, int capacity) => $"カバン　{usedSlots}/{capacity}マス";
        public static string BagEmpty => "（空）";
        public static string InventoryItemLine(int index, string headline) => $"{index}. {headline}";
        public static string EatHint => "\n数字キーで飲食・使用";

        // ── 行動トースト（ActionToast） ──────────────────────────
        // 習得の瞬間は明示的に通知する（第六節）。習得したのはルールであって数値ではない。
        public static string KnackAcquired(string knackName) => $"コツを覚えた：{knackName}";

        // 目標の達成も明示的に通知する（第八節）。
        public static string MilestoneAchieved(string milestoneName) => $"めあてを果たした：{milestoneName}";

        // ── 目標一覧（MilestoneHud） ─────────────────────────────
        public static string MilestoneListHeader => "めあて";
        public static string MilestoneLine(string name, bool achieved) =>
            achieved ? $"・<s>{name}</s>（済）" : $"・{name}";

        // ── インタラクトの促し（InteractionPrompt） ──────────────
        public static string Rummaging(string progressBar) => $"漁っている… {progressBar}　［動くと手を止める］";
        public static string Interactable(string description) => $"{description}　［E］";
        public static string Discardable(string description) => $"{description}　［G］";
    }
}
