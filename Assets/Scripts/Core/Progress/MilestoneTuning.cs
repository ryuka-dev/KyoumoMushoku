namespace KyoumoMushoku.Core.Progress
{
    /// <summary>
    /// 段階目標の構造的定数。垂直スライスの構成（3ゲームデイ・第九節）に紐づく値であり、
    /// プレイテストで動かす平衡数値ではないため、静的クラスに置く。
    /// </summary>
    public static class MilestoneTuning
    {
        /// <summary>生存目標の日数。この日数を生き延びた（＝翌日の朝を迎えた）ら達成（第八節）。</summary>
        public const int SurvivalDays = 3;
    }
}
