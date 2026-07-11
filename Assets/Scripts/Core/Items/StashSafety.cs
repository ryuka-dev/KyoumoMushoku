namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫の安全性（第十二節）。安全性は保管庫イベントの発生確率にかける係数として表す。
    /// 係数は [0,1]：1 は無防備（確率そのまま）、小さいほど安全（発生しにくい）、0 は完全に安全（発生しない）。
    ///
    /// 種別ごとに基礎の安全性が違い（段ボール箱は低い・コインロッカーは高い）、場所代を払うと上がる。
    /// 「安全性を上げてイベント確率を下げる」という第十二節の対抗手段を、確率への一つの入力として素直に写す。
    /// 数値は叩き台であり、プレイテストで調整する（第十五節）。
    /// </summary>
    public static class StashSafety
    {
        /// <summary>段ボール箱・場所代を払った日の発生確率係数（低いほど安全）。無防備の 1 に対して大きく下げる。</summary>
        public const float CardboardBoxRentPaidMultiplier = 0.4f;

        /// <summary>
        /// コインロッカー・使用料を払った日の発生確率係数（第十二節「高安全」）。段ボール箱より更に大きく下げる。
        /// 使用料が切れた日は無防備（1）に戻る＝「有料である限り高安全」。
        /// </summary>
        public const float CoinLockerRentPaidMultiplier = 0.1f;

        /// <summary>
        /// 保管庫イベントの発生確率にかける安全性の係数。種別と、その日の場所代・使用料の支払状況で決まる。
        /// どの種別も無防備（1）が既定で、料金を払った日だけ下がる（段ボール箱より コインロッカーの方が安全）。
        /// 未知の種別は素通し（1）にとどめる。
        /// </summary>
        public static float EventChanceMultiplier(StashKind kind, bool rentActive) => kind switch
        {
            StashKind.CardboardBox => rentActive ? CardboardBoxRentPaidMultiplier : 1f,
            StashKind.CoinLocker => rentActive ? CoinLockerRentPaidMultiplier : 1f,
            _ => 1f,
        };
    }
}
