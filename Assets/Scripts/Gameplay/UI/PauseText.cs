using KyoumoMushoku.Gameplay.App;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// ポーズ画面のプレイヤー向け文字列を集約する。語法はメソッド本体に閉じ込め、将来の多言語化は
    /// 本体をテーブル参照へ差し替えるだけで済む（ローカライズの口子）。
    /// </summary>
    public static class PauseText
    {
        // ── 本メニュー ───────────────────────────────────────────
        public static string Header => "ポーズ";
        public static string Resume => "ゲームに戻る";
        public static string Settings => "設定";
        public static string Quit => "ゲームを終了";
        public static string MainHint => "数字キーで選ぶ　Esc で戻る";

        // ── 設定（2 階層目） ─────────────────────────────────────
        public static string SettingsHeader => "設定";
        public static string OpenSaveFolder => "セーブデータの場所を開く";
        public static string SettingsHint => "数字キーで選ぶ　Esc で戻る";

        /// <summary>フレーム上限の行。無制限は語で示し、数値を偽らない（第三節）。</summary>
        public static string FrameLimitLine(int limit)
        {
            var value = limit == FrameRateSetting.Unlimited ? "無制限" : $"{limit} fps";
            return $"フレーム上限：{value}　［1 で変更］";
        }

        // ── 事後の報せ ───────────────────────────────────────────
        public static string SaveFolderOpened => "セーブデータの場所を開いた。";
        public static string SaveFolderFailed => "セーブデータの場所を開けなかった。";
    }
}
