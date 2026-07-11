using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Progress;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>UI に出す日本語ラベル。表示名は識別子とは別概念であり、ここに集約する。</summary>
    public static class GameTextLabels
    {
        public static string Phase(DayPhase phase) => phase switch
        {
            DayPhase.Day => "昼",
            DayPhase.Dusk => "夕方",
            DayPhase.Night => "夜",
            _ => "―",
        };

        public static string Zone(AlertZoneId zone) => zone switch
        {
            AlertZoneId.Quiet => "静穏",
            AlertZoneId.Residential => "生活",
            AlertZoneId.Commercial => "商業",
            _ => "―",
        };

        /// <summary>
        /// 警察の段階（第五節）。プレイヤーにこの語を直接見せる場所はまだ無い。
        /// 段階は警官の振る舞いと台詞から読み取られるべきものであり、ここは診断と将来の用途のために置く。
        /// </summary>
        public static string PoliceStage(PoliceStage stage) => stage switch
        {
            Core.Police.PoliceStage.Noticing => "注意",
            Core.Police.PoliceStage.Warning => "警告",
            Core.Police.PoliceStage.Pursuing => "追い出し",
            _ => "―",
        };

        /// <summary>コツの表示名（第六節）。識別子とは別概念であり、ここに集約する。</summary>
        public static string Knack(KnackId id) => id switch
        {
            KnackId.SpotDuds => "あたりの見分け方",
            KnackId.SteadyHands => "手を止めない",
            KnackId.IronStomach => "鉄の胃袋",
            KnackId.StreetSleeper => "路上の寝方",
            KnackId.FamiliarFace => "通りすがりの顔",
            _ => "―",
        };

        /// <summary>段階目標の表示名（第八節）。識別子とは別概念であり、ここに集約する。</summary>
        public static string Milestone(MilestoneId id) => id switch
        {
            MilestoneId.SurviveThreeDays => "3日間生存する",
            MilestoneId.FirstInnStay => "初めて安宿に泊まる",
            MilestoneId.BuyBackpack => "バックパックを購入する",
            _ => "―",
        };

        public static string SanityTier(SanityTier tier) => tier switch
        {
            Core.Survival.SanityTier.Elated => "上機嫌",
            Core.Survival.SanityTier.Normal => "普通",
            Core.Survival.SanityTier.Dulled => "気分が沈む",
            Core.Survival.SanityTier.Broken => "精神崩壊",
            _ => "―",
        };
    }
}
