using KyoumoMushoku.Core.Items;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 保管庫（段ボール箱・コインロッカー）と、路地裏の先輩ホームレスの玩家向け文字列を集約する
    /// （第十二・十四節）。予告・事後説明・場所代・出し入れ・先輩の台詞をここに閉じ込める。
    ///
    /// 語法はメソッド本体に閉じ込め、呼び出し側は意味でメソッドを呼ぶ。将来の多言語化はメソッド本体を
    /// テーブル参照へ差し替えるだけで済む（メソッド名がそのまま参照キー）。診断ログ・ツールチップは対象外。
    /// </summary>
    public static class StashText
    {
        // ── 保管庫の呼び名（種別） ───────────────────────────────
        public static string KindLabel(bool coinLocker) => coinLocker ? "コインロッカー" : "段ボール箱";
        public static string RentLabel(bool coinLocker) => coinLocker ? "使用料" : "場所代";

        /// <summary>設置場所が無いときのパネル見出しの呼び名。</summary>
        public static string DefaultStashLabel => "保管庫";

        /// <summary>設置場所が無いときの、出し入れ側の短い呼び名。</summary>
        public static string ShortStashLabel => "箱";

        // ── StashSpot.Describe（インタラクトの説明） ─────────────
        public static string OpenStash(string kindLabel, int usedSlots, int capacity) =>
            $"{kindLabel}（{usedSlots}/{capacity}マス）を開ける";
        public static string OpenCoinLocker => "コインロッカーを開ける";
        public static string PlaceCardboardHere => "ここに段ボールを置く";
        public static string PlaceHint => "（段ボールがあればここに置ける）";

        // ── 設置チャネルの結果 ───────────────────────────────────
        public static string CannotPlaceHere => "ここには置けない。";
        public static string NoCardboardToPlace => "置く段ボールがない。";
        public static string CardboardPlaced => "段ボール箱を置いた。";

        // ── 予告（保証チャネル・貼り紙／第十二節） ───────────────
        public static string Forecast(StashEventKind kind) => kind switch
        {
            StashEventKind.CityCleaning => "清掃予告の貼り紙：明日の朝、この辺りの清掃が入る",
            StashEventKind.ScavengedByPeers => "荒らされた足跡がある……明日あたり誰かに漁られそうだ",
            StashEventKind.PoliceRemoval => "昼間、警官がこの辺りを下見していた。明日、撤去されるかもしれない",
            _ => string.Empty,
        };

        // ── 事後説明（貼り紙／第十四節） ─────────────────────────
        public static string Aftermath(StashEventKind kind, int lost) => kind switch
        {
            StashEventKind.CityCleaning => $"清掃が入った（{lost}点を失った）",
            StashEventKind.ScavengedByPeers => $"同業者に漁られた（{lost}点を失った）",
            StashEventKind.PoliceRemoval => $"警察に撤去された（{lost}点を失い、警戒度が上がった）",
            _ => string.Empty,
        };

        // ── 先輩ホームレスの台詞（第十二・十四節） ───────────────
        public static string ElderPlacementRule => "ここ、たまに清掃が入るぞ。貯め込みすぎると狙われる。";
        public static string ElderRentPaid => "あいよ、今日のぶんは確かに。ここは見といてやる。";
        public static string ElderRentForfeited => "箱ごと持ってくのか。今日払ったぶんは、もう戻らねえぞ。";
        public static string ElderHoardNag => "その箱、膨らみすぎだ。目立つぞ。";

        public static string ElderAftermath(StashEventKind kind, int lost) => kind switch
        {
            StashEventKind.CityCleaning => $"昨夜、清掃が入っただろ。貼り紙が出てたはずだ（{lost}点）。",
            StashEventKind.ScavengedByPeers => $"誰かに漁られたな。膨らんだ箱は目立つ（{lost}点）。",
            StashEventKind.PoliceRemoval => $"警察に持ってかれたか。ここらで目立ちすぎたんだ（{lost}点）。",
            _ => string.Empty,
        };

        // 噂話（SAN 70 以上）は前倒しの助言。有効な対抗手段を世界の言葉で示す（第十二節）。
        public static string ElderRumor(StashEventKind kind) => kind switch
        {
            StashEventKind.CityCleaning => "明日、清掃が入るらしい。大事な物は今のうちに持って出な。",
            StashEventKind.ScavengedByPeers => "この辺で漁られる話をよく聞く。抱えて持ち歩くのも手だ。",
            StashEventKind.PoliceRemoval => "警官が下見してた。明日は箱を空にしておけ。",
            _ => string.Empty,
        };

        // ── StashPanel（出し入れの面） ───────────────────────────
        public static string PanelHeader(string kindLabel) =>
            $"＝ {kindLabel} ＝　［Tab：預ける↔引き出す］　［E／Esc：閉じる］";

        public static string DepositHeading(bool active, string label, int usedSlots, int capacity) =>
            $"{(active ? "▶" : "　")} 預ける（カバン → {label}）　カバン {usedSlots}/{capacity}マス";
        public static string WithdrawHeading(bool active, string label, int usedSlots, int capacity) =>
            $"{(active ? "▶" : "　")} 引き出す（{label} → カバン）　{label} {usedSlots}/{capacity}マス";

        public static string BagEmpty => "（カバンは空）";
        public static string StashEmpty(string label) => $"（{label}は空）";

        public static string ItemLine(int index, bool numbered, string name) =>
            $"　　{(numbered ? $"{index}." : "・")} {name}";

        // 場所代・使用料の状況表示
        public static string RentPaidStatus(string rentLabel) =>
            $"{rentLabel}：本日ぶん支払い済み（今日はここが少し安全だ）";
        public static string RentUnpaidStatus(string rentLabel, int costYen) =>
            $"{rentLabel}：未払い　［R：{costYen}円 払う］";

        // 回収（第十二節「別の場所へ移す」）
        public static string ReclaimReady => "空の箱　［G：回収して担ぐ］";
        public static string ReclaimBlocked => "空の箱（回収するには背負いを空けて）";

        // 場所代の支払い結果
        public static string RentPaid(string rentLabel, int costYen) =>
            $"{rentLabel} {costYen}円を払った。今日はここが少し安全だ。";
        public static string RentAlreadyPaid => "今日のぶんはもう払ってある。";
        public static string RentCannotAfford(string rentLabel) => $"{rentLabel}が払えない。";

        // 出し入れの反馈
        public static string BoxFull => "箱がいっぱいだ。";
        public static string Deposited(string name) => $"{name}を箱に入れた。";
        public static string BagFull => "カバンがいっぱいだ。";
        public static string Withdrew(string name) => $"{name}を取り出した。";
    }
}
