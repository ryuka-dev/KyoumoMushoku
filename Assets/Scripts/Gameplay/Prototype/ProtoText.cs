namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片（Docs/一人称試作.md）のプレイヤー向け文字列。ローカライズの規約どおり、文字列はここにだけ書く。
    /// 店主の台詞・状態の名前・食品の見出しは本編のテキストモジュール（ShopText・HudText・GameTextLabels・FoodCardText）を使う。
    /// </summary>
    public static class ProtoText
    {
        public static string Controls =>
            "WASD 移動／Shift 走る／マウス 視点／左クリック 叩く・掴む・使う／数字キー 飲食／Q＋数字 捨てる／Esc カーソル";

        public static string Status(int day, string phase, int yen, string mood) =>
            $"{day}日目　{phase}\n所持金 {yen}円\n気分 {mood}";

        public static string Opened => "ふたが開いた！";
        public static string BagRipped => "袋が裂けた！";
        public static string Nothing => "……空っぽだった。";
        public static string Caught(string name) => $"{name}を掴んだ。";
        public static string NoRoom(string name) => $"{name}はカバンに入らない。（Q＋数字で捨てられる）";
        public static string Discarded(string name) => $"{name}を捨てた。";
        public static string DisposalBurst => "袋が破れた！";
        public static string Depleted => "もう何も出ない。";

        // ── 店 ───────────────────────────────────────────────
        public static string SellPrompt(int salvageYen, int remainingYen) =>
            $"廃品を売る（カバンの廃品 {salvageYen}円・今日はあと {remainingYen}円まで）";
        public static string BuyPrompt(string name, string price) => $"{name}を買う：{price}";
        public static string BuyBackpackPrompt(string name, string price, int extraSlots) =>
            $"{name}を買う：{price}（カバン +{extraSlots}）";
        public static string Price(int yen) => $"{yen}円";
        public static string ShopDoorPrompt => "コンビニ：入る";
        public static string ShopTitle => "コンビニ";
        public static string ShopGreeting => "……いらっしゃいませ。";
        public static string ShopGreetingBeforeDisposal => "……いらっしゃいませ。（今夜も弁当、だいぶ余りそうだな）";
        public static string ClerkMutterAtDusk => "店員「暗くなったら、裏に出しとくか……」";
        public static string ClerkDropsDisposal => "店員「……廃棄、っと」";
        public static string ShopWallet(int yen, int used, int capacity) => $"所持金 {yen}円　カバン {used}/{capacity}";
        public static string ShopLeave => "店を出る［Esc］";
        public static string BackpackOwned(string name) => $"{name}：購入済み";

        // ── 水・飲食 ─────────────────────────────────────────
        public static string FaucetPrompt => "水道：水を飲む";
        public static string Drank => "水を飲んだ。";
        public static string NotThirsty => "喉は渇いていない。";
        public static string Consumed(string name, bool wasRotten) =>
            wasRotten ? $"{name}を口にした。……腐っていた。" : $"{name}を口にした。";
        public static string NotEdible(string name) => $"{name}は口にできない。";

        // ── 寝床と一日の区切り ───────────────────────────────
        public static string BenchPrompt => "ベンチ：横になって夜を越す";
        public static string BenchTooEarly => "ベンチ：まだ明るい。寝るには早い";
        public static string WokeUp => "朝になった。";
        public static string MorningTitle(int day) => $"{day}日目の朝";
        public static string MorningBody(int caught, int yen, string mood) =>
            $"昨日はゴミ箱から {caught} 個を掴んだ。\n所持金 {yen}円　気分 {mood}\n\n今日も無職。";
        public static string CollapsedTitle => "倒れた";
        public static string CollapsedBody(int day) => $"{day}日目。路上で意識が途切れた。";
        public static string ContinueButton => "続ける";
        public static string RestartButton => "最初から";
        public static string QuitButton => "終わる";

        // ── 自販機 ───────────────────────────────────────────
        public static string VendingPrompt(string name, string price) => $"自販機：{name}を買う：{price}";
        public static string VendingBought(string name) => $"ガコン。{name}が出てきた。";
        public static string VendingCannotAfford => "小銭が足りない。";
        public static string VendingBagFull => "カバンに入らない。";
        public static string CoinSearchPrompt => "自販機の下：手を突っ込んで探る";
        public static string CoinFound(int yen) => $"{yen}円玉が落ちていた。";
        public static string CoinNone => "……何も無かった。";
        public static string CoinAlreadySearched => "今日はもう探った。";

        // ── 警官 ─────────────────────────────────────────────
        public static string PoliceSays(string line) => $"警官「{line}」";
        public static string PoliceStatus(string stage) => $"警官：{stage}";

        // ── 珍しさ ───────────────────────────────────────────
        public static string Rare => "珍しい";
        public static string Uncommon => "やや珍しい";
        public static string WithRarity(string name, string rarity) => $"{name}（{rarity}）";
    }
}
