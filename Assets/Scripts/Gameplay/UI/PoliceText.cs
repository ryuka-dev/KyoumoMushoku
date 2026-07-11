namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 警官の世界内の台詞（第五・十四節）。段階（注意→警告→追い出し）と捕縛の一言を集約する。
    /// 台詞は誰が言ったか分かるよう頭上に出す（<see cref="NpcSpeech"/>）。語法はここに閉じ込め、
    /// 将来の多言語化はメソッド本体をテーブル参照へ差し替えるだけで済む。
    /// </summary>
    public static class PoliceText
    {
        public static string Notice => "……おい。";
        public static string Warning => "ここは君の居場所じゃない。立ち去りなさい。";
        public static string Pursue => "待ちなさい！";
        public static string Capture => "二度とここで漁るな。";
        public static string CaptureWithSeizure => "腐ったものは預かる。二度とここで漁るな。";
    }
}
