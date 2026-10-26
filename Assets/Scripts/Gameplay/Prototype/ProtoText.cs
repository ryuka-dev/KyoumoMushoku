namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の打撃試作（Docs/一人称試作.md）のプレイヤー向け文字列。試作でもローカライズの規約は守る。
    /// 本採用になれば、本編のテキストモジュールへ移して作り直す。
    /// </summary>
    public static class ProtoText
    {
        public static string Controls =>
            "WASD 移動／マウス 視点／左クリック 叩く・掴む／N 昼夜／R ゴミ箱を戻す／Esc カーソル";

        public static string Bag(int used, int capacity) => $"カバン {used}/{capacity}";
        public static string BagLine(string name) => $"・{name}";
        public static string TimeOfDay(bool night) => night ? "夜" : "昼";

        public static string Opened => "ふたが開いた！";
        public static string Nothing => "……空っぽだった。";
        public static string Caught(string name) => $"{name}を掴んだ。";
        public static string NoRoom(string name) => $"{name}はカバンに入らない。";
        public static string Refilled => "ゴミ箱が元に戻った。";
        public static string Depleted => "もう何も出ない。";

        public static string Money(int yen) => $"所持金 {yen}円";
        public static string SellPrompt(int salvageYen, int remainingYen) =>
            $"回収箱：廃品を売る（カバンの廃品 {salvageYen}円・本日あと {remainingYen}円）";
        public static string BuyPrompt(string name, int priceYen, int extraSlots) =>
            $"{name}を買う：{priceYen}円（カバン +{extraSlots}）";
        public static string BackpackOwned(string name) => $"{name}：購入済み";

        public static string Rare => "珍しい";
        public static string Uncommon => "やや珍しい";
        public static string WithRarity(string name, string rarity) => $"{name}（{rarity}）";
    }
}
