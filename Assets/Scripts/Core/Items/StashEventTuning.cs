namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫イベントの確率と損失の数値（叩き台）。プレイテストで調整する（第十五節）。
    ///
    /// 設計の要点は「警戒度は複数入力の1つ」であること。だから発生確率は警戒度と保管量の両方から作り、
    /// どちらか一方だけでは高くならない。損失は種別で異なる（清掃＝大部分／同業者＝一部／撤去＝ほぼ全部）。
    /// </summary>
    public static class StashEventTuning
    {
        /// <summary>警戒度（0〜100）が満額のとき、発生確率に足す寄与。</summary>
        public const float AlertContribution = 0.6f;

        /// <summary>保管量がこのマス数に達すると、量による寄与が満額になる。</summary>
        public const int FullnessReferenceSlots = 12;

        /// <summary>保管量が満のとき、発生確率に足す寄与。</summary>
        public const float FullnessContribution = 0.25f;

        /// <summary>警察の撤去が候補に入り始める警戒度。これ未満では撤去は起きない（顔をまだ覚えられていない）。</summary>
        public const float PoliceRemovalAlertFloor = 50f;

        /// <summary>市の清掃で失う割合（大部分）。</summary>
        public const float CityCleaningLossFraction = 0.7f;

        /// <summary>同業者に漁られて失う割合（一部）。</summary>
        public const float ScavengedLossFraction = 0.3f;

        /// <summary>警察の撤去で失う割合（ほぼ全部）。</summary>
        public const float PoliceRemovalLossFraction = 0.9f;

        /// <summary>警察の撤去は、保管庫のあるゾーンの警戒度をさらに上げる（第十二節）。</summary>
        public const float PoliceRemovalAlertRaise = 15f;

        /// <summary>種別ごとの損失割合。</summary>
        public static float LossFractionFor(StashEventKind kind) => kind switch
        {
            StashEventKind.CityCleaning => CityCleaningLossFraction,
            StashEventKind.ScavengedByPeers => ScavengedLossFraction,
            StashEventKind.PoliceRemoval => PoliceRemovalLossFraction,
            _ => 0f,
        };
    }
}
