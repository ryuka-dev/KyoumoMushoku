using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>品の珍しさの段（`Docs/切片の品目.md` 第三節）。出やすさから導かず、品そのものが持つ。</summary>
    public enum LootTier
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Jackpot = 3,
    }

    /// <summary>叩いている間（ふたが開く前）に、プレイヤーが中身についてどこまで読めるか。</summary>
    public enum HitTell
    {
        /// <summary>何も分からない。どの箱も同じ揺れ・同じ音に感じる。</summary>
        Nothing = 0,

        /// <summary>空か詰まっているかだけ（揺れと音）。</summary>
        WeightOnly = 1,

        /// <summary>それに加えて、珍しい物が入っていれば光が漏れる。珍しいか大当たりかは分からない。</summary>
        SomethingGood = 2,

        /// <summary>光の色で、珍しいか大当たりかまで分かる。上機嫌のときだけ。</summary>
        Detailed = 3,
    }

    /// <summary>ふたの隙間から漏れる光。</summary>
    public enum HitGlint
    {
        None = 0,

        /// <summary>「何か良い物がある」。珍しいと大当たりを区別しない。</summary>
        Plain = 1,

        Rare = 2,
        Jackpot = 3,
    }

    /// <summary>
    /// SAN＝都市を読む力（第三節）を、叩いた手応えに当てる。中身は叩く前に確定しており（再抽選しない）、
    /// ここが決めるのはそれをどこまで見せるかだけである。読めないときは合図が消えるだけで、偽の合図は出さない。
    /// 段の境は食品カードの `??` と同じ（上機嫌・はっきり見える・崩壊）。
    /// </summary>
    public static class HitTellSight
    {
        public static HitTell Read(float sanity)
        {
            if (sanity < SanityScale.BreakdownThreshold)
            {
                return HitTell.Nothing;
            }

            if (sanity < SanityScale.ClearSightThreshold)
            {
                return HitTell.WeightOnly;
            }

            return sanity >= SanityScale.ElatedThreshold ? HitTell.Detailed : HitTell.SomethingGood;
        }

        /// <summary>箱の中で一番良い品の段から、漏れる光を決める。珍しい物が無ければ、どの読みでも光らない。</summary>
        public static HitGlint Glint(HitTell tell, LootTier best)
        {
            if (best < LootTier.Rare || tell < HitTell.SomethingGood)
            {
                return HitGlint.None;
            }

            if (tell == HitTell.SomethingGood)
            {
                return HitGlint.Plain;
            }

            return best == LootTier.Jackpot ? HitGlint.Jackpot : HitGlint.Rare;
        }

        /// <summary>揺れと音から、空か詰まっているかが分かるか。</summary>
        public static bool FeelsWeight(HitTell tell) => tell >= HitTell.WeightOnly;
    }
}
