namespace KyoumoMushoku.Core.Police
{
    /// <summary>無料の就寝場所が、静穏ゾーンの警戒度に応じて辿る状態（第五節）。</summary>
    public enum SleepSpotState
    {
        /// <summary>普通に使える。</summary>
        Open = 0,

        /// <summary>苦情の貼り紙が出ている。まだ使えるが、これが撤去の予告である（第十四節）。</summary>
        Warned = 1,

        /// <summary>撤去された。使えない。警戒度が下がれば数日で戻る。</summary>
        Removed = 2,
    }

    /// <summary>
    /// 静穏ゾーンの警戒度が駆動する唯一の事項（第五節）：無料の就寝場所の撤去。
    ///
    /// 同じベンチで寝続けると近隣から苦情が出て、やがて自治体がベンチを撤去する（敵対的建築）。
    /// ここで働くのは警官という個人ではなく、無職者が居着いた場所を都市が無感情に閉ざす圧力である。
    ///
    /// 撤去は確率抽選ではなく、警戒度が閾値を越えたか否かで決まる。恒久でもない。
    /// 撤去されると寝られないので警戒度は上がらず（<c>ZoneAlertTuning.FreeSleepRaise</c> が加わらない）、
    /// 時間経過で単調に下がって数日で <see cref="SleepSpotState.Removed"/> を抜ける。したがって往復（ちらつき）は起きない。
    ///
    /// 撤去に先立って <see cref="SleepSpotState.Warned"/> の段で苦情の貼り紙が出る。これが事前の予告であり、
    /// プレイヤーが受け取るのは理不尽な故障ではなく読める警告になる（第十四節）。
    ///
    /// 閾値は叩き台であり、プレイテストで調整する。無料就寝は1泊 +20（<c>ZoneAlertTuning.FreeSleepRaise</c>）
    /// 積み上がり、他所で寝れば就寝＋時間で毎日およそ一割ずつ減る。数泊で貼り紙、その先で撤去に達する尺で選ぶ。
    /// </summary>
    public static class SleepSpotClosure
    {
        public const float WarnThreshold = 40f;
        public const float RemoveThreshold = 60f;

        public static SleepSpotState For(float quietAlertLevel)
        {
            if (quietAlertLevel >= RemoveThreshold)
            {
                return SleepSpotState.Removed;
            }

            return quietAlertLevel >= WarnThreshold ? SleepSpotState.Warned : SleepSpotState.Open;
        }
    }
}
