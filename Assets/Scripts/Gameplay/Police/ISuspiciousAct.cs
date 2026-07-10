namespace KyoumoMushoku.Gameplay.Police
{
    /// <summary>
    /// 人目につく行為。警官はこれを通してのみ「プレイヤーがいま何をしているか」を知る。
    ///
    /// この境界がなければ、警官はゴミ箱（漁りモジュール）を直接知ることになる。
    /// 保管庫を掘る、段ボールを設置する（第十二節）といった行為も、後から同じ口を通して載る。
    /// </summary>
    public interface ISuspiciousAct
    {
        /// <summary>この行為を視界内で続けている間、毎秒どれだけ注目度が上がるか。</summary>
        float SuspicionPerSecond { get; }
    }
}
