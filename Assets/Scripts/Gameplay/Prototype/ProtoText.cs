using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Gameplay.UI;

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

        // ── 照準の案内（何を押せば何が起きるか。案内は事実だけを言う） ──
        public static string ClickPrompt(string prompt) => $"［左クリック］{prompt}";
        public static string TrashCanPrompt => "ゴミ箱：ふたが開くまで叩く";
        public static string TrashBagPrompt => "ゴミ袋：破れるまで叩く";
        public static string GrabPrompt(string name) => $"掴む：{name}";
        public static string WatchedPrompt => "警官に見られている：いまは叩けない";

        // ── コツ（生活の知恵・GameDesign 第六節）。覚えた瞬間の独り言と、朝の区切り・結算の一行 ──
        public static string HandsStopped => "……見られている。いまは手が出せない。";
        public static string KnackLearned(KnackId id) => id switch
        {
            KnackId.SpotDuds => "……叩いた手応えで、中身の見当がつくようになってきた。",
            KnackId.SteadyHands => "……見られていても、もう手は止めない。止めたところで、どうせ追い払われる。",
            KnackId.IronStomach => "……腹を壊さずに済んだ。胃袋が、この暮らしに慣れてきた。",
            KnackId.StreetSleeper => "……ベンチでの眠り方が分かってきた。前より疲れが取れる。",
            _ => "……この街の勝手が、少し分かってきた。",
        } + $"（コツ：{GameTextLabels.Knack(id)}）";
        public static string KnacksLine(System.Collections.Generic.IReadOnlyList<KnackId> knacks, bool learnedYesterday)
        {
            if (knacks == null || knacks.Count == 0)
            {
                return "";
            }

            var names = new System.Text.StringBuilder();
            foreach (var id in knacks)
            {
                names.Append(names.Length > 0 ? "・" : "").Append(GameTextLabels.Knack(id));
            }

            return learnedYesterday ? $"コツを覚えた：{names}\n" : $"覚えたコツ：{names}\n";
        }

        // ── 最初の一手（一日目だけの独り言） ──
        public static string FirstStepHungry => "……腹減った。ゴミ箱でも漁るか。";
        public static string FirstStepAimCan => "ふたが固い。……叩けば開くか。";
        public static string FirstStepNudge => "コンビニの前に、ゴミ箱があったな。";

        // ── 店 ───────────────────────────────────────────────
        public static string SellPrompt(int salvageYen, int remainingYen) =>
            $"廃品を売る（カバンの廃品 {salvageYen}円・今日はあと {remainingYen}円まで）";
        public static string BuyPrompt(string name, string price) => $"{name}を買う：{price}";
        public static string BuyBackpackPrompt(string name, string price, int extraSlots) =>
            $"{name}を買う：{price}（カバン +{extraSlots}）";
        public static string Price(int yen) => $"{yen}円";
        public static string ShopDoorPrompt => "コンビニ：入る";
        public static string RecycleSign => "リサイクル　買取";
        public static string RecyclePrompt => "リサイクルショップ：値打ち物を買い取る";
        public static string RecycleSellPrompt(int count, string price) => $"リサイクルショップ：値打ち物を売る（{count}点・{price}）";
        public static string RecycleSold(int count, int paidYen) => $"店主「{count}点で{paidYen}円。まいど」";
        public static string RecycleNothing => "店主「うちは値打ち物しか買わないよ。缶や瓶はコンビニへ」";
        public static string RecycleHint => "これは……アーケードのリサイクル屋なら、金になるかもしれない。";
        public static string ShopTitle => "コンビニ";
        public static string ShopGreeting => "……いらっしゃいませ。";
        public static string ShopGreetingBeforeDisposal => "……いらっしゃいませ。（今夜も弁当、だいぶ余りそうだな）";
        public static string ShopGreetingNoDisposal => "……いらっしゃいませ。（今日はよく売れたな。廃棄はほとんど無いか）";
        public static string ClerkMutterAtDusk => "店員「暗くなったら、裏に出しとくか……」";
        public static string ClerkMutterNoDisposal => "店員「今日は廃棄、ほとんど出ないな……」";
        public static string ClerkDropsDisposal => "店員「……廃棄、っと」";
        public static string ShopWallet(int yen, int used, int capacity) => $"所持金 {yen}円　カバン {used}/{capacity}";
        public static string ShopLeave => "店を出る［Esc］";
        public static string BackpackOwned(string name) => $"{name}：購入済み";
        public static string WorkPrompt => "はたらく：レジ打ち（気分と空腹を削る・時間が進む）";
        public static string WorkDoneToday => "はたらく：今日のシフトはもう終わった";
        public static string WorkControls => "左クリック／Space：打つ　　Esc：やめる（何も減らない）";
        public static string WorkHit => "ピッ";
        public static string WorkMiss => "……打ち損じた";

        // ── 水・飲食 ─────────────────────────────────────────
        public static string FaucetPrompt => "水道：水を飲む";
        public static string Drank => "水を飲んだ。";
        public static string NotThirsty => "喉は渇いていない。";
        public static string ToiletPrompt => "公衆トイレ：手洗いの水を飲む（気が滅入る）";
        public static string DrankToiletWater => "トイレの手洗いで水を飲んだ。……喉は潤ったが、気が滅入る。";
        public static string Consumed(string name, bool wasRotten) =>
            wasRotten ? $"{name}を口にした。……腐っていた。" : $"{name}を口にした。";
        public static string NotEdible(string name) => $"{name}は口にできない。";
        public static string ReadThrough(string name) => $"{name}を読みふけった。少し気が紛れた。";
        public static string Scratched(int prizeYen) =>
            prizeYen <= 0 ? "スクラッチを削った。……はずれ。" : $"スクラッチを削った。……{prizeYen}円、当たった。";
        public static string Shouldered(string name) => $"{name}を背負った。重い。走れない。（G で降ろす）";
        public static string AlreadyCarrying(string name) => $"もう背負っている。{name}は担げない。（G で降ろす）";
        public static string Unloaded(string name) => $"{name}を降ろした。";
        public static string CarryingLine(string name) => $"背負っている：{name}（ベンチで寝ると敷く・G で降ろす）";
        public static string GlovesFound => "軍手だ。これなら、ふたを叩くのも一回少なくて済む。";
        public static string CapFound => "キャップだ。目深にかぶれば、少しは目立たない。";
        public static string EquipmentInBag(string name) => $"{name}は、カバンにあるあいだ役に立つ。";
        public static string FirstButt => "……前の俺なら、こんな物は拾わなかった。";
        public static string PurseOpened(int yen) => $"小銭入れだ。中に{yen}円入っていた。";

        // ── 寝床と一日の区切り ───────────────────────────────
        public static string BenchPrompt => "ベンチ：横になって夜を越す";
        public static string BenchTooEarly => "ベンチ：まだ明るい。寝るには早い";
        public static string WokeUp => "朝になった。";
        public static string WokeUpAtInn => "朝になった。布団で眠った。体も頭も、すっかり軽い。";
        public static string InnSign(int yen) => $"旅館　素泊まり{yen}円";
        public static string InnPrompt(string price) => $"安宿：泊まる（{price}・すべて回復する）";
        public static string InnTooEarly(string price) => $"安宿：夕方から泊まれる（{price}）";
        public static string InnCannotAfford => "金が足りない。";
        public static string WokeUpOnCardboard => "朝になった。段ボールのおかげで、少しはましに眠れた。朝露で湿って、もう使えない。";
        public static string LateNightDrowsy => "……眠い。頭が回らなくなってきた。";
        public static string LateNightExhausted => "……まずい。目を開けているのがつらい。";
        public static string MorningTitle(int day) => $"{day}日目の朝";
        public static string MorningBody(int caught, int yen, string mood, string knacksLine) =>
            $"昨日はゴミ箱から {caught} 個を掴んだ。\n所持金 {yen}円　気分 {mood}\n{knacksLine}\n今日も無職。";
        public static string FinalTitle(int days) => $"{days}日目の夜が明けた";
        public static string FinalBody(int days, int caught, int yen, int capacity, string mood, int innNights, int clinicVisits,
            string knacksLine) =>
            $"{days}日間で、ゴミ箱から {caught} 個を掴んだ。\n所持金 {yen}円　カバン {capacity}マス　気分 {mood}\n"
            + NightsLine(innNights, clinicVisits)
            + knacksLine
            + "\n今日も無職。\n\n（体験版はここで一区切り。続けて遊ぶこともできる）";
        static string NightsLine(int innNights, int clinicVisits)
        {
            var inn = innNights > 0 ? $"安宿に {innNights} 泊した。" : "";
            var clinic = clinicVisits > 0 ? $"診療所に {clinicVisits} 度運ばれた。" : "";
            return inn.Length + clinic.Length > 0 ? inn + clinic + "\n" : "";
        }

        // ── 診療所（倒れたら運ばれる・生きていれば手当てを受けられる） ──
        public static string ClinicSign => "内科・外科　みなみ診療所";
        public static string ClinicPrompt(string price) => $"診療所：手当てを受ける（{price}・体力が全回復する）";
        public static string ClinicNotNeeded => "どこも悪くない。";
        public static string ClinicCannotAfford => "金が足りない。";
        public static string ClinicTreated => "手当てを受けた。体が軽い。";
        public static string CollapsedTitle => "倒れた";
        public static string CollapsedBody(int day, int feeYen, int paidYen, bool seizedRotten)
        {
            var paid = paidYen >= feeYen ? $"医療費 {paidYen}円を払った。" : $"医療費は {feeYen}円。持ち合わせの {paidYen}円を、あるだけ払った。";
            return $"{day}日目。路上で意識が途切れた。\n気がつくと、診療所のベッドの上だった。\n{paid}\n"
                   + (seizedRotten ? "腐った物は、捨てられていた。\n" : "")
                   + "体は戻ったが、気分は沈んだ。喉と腹は、倒れる前のまま。";
        }
        public static string WokeUpAtClinic => "……診療所の前に出された。向かいの公園に、水道がある。";
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
        public static string Jackpot => "大当たり";

        /// <summary>宙の物の名前に添える段の名前。並なら null（何も添えない）。</summary>
        public static string TierName(KyoumoMushoku.Core.Foraging.LootTier tier) => tier switch
        {
            KyoumoMushoku.Core.Foraging.LootTier.Jackpot => Jackpot,
            KyoumoMushoku.Core.Foraging.LootTier.Rare => Rare,
            KyoumoMushoku.Core.Foraging.LootTier.Uncommon => Uncommon,
            _ => null,
        };
        public static string WithRarity(string name, string rarity) => $"{name}（{rarity}）";
    }
}
