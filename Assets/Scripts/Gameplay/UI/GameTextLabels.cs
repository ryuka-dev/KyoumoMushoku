using KyoumoMushoku.Core.DayCycle;
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
