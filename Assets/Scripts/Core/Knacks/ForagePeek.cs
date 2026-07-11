using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Knacks
{
    /// <summary>ゴミ箱の次の1回について、プレイヤーがどこまで読めるか（あたりの見分け方・第六節）。</summary>
    public enum ForagePeek
    {
        /// <summary>読めない。コツを持たない。何も示さない。</summary>
        Hidden = 0,

        /// <summary>コツはあるが SAN が低すぎて集中できない。`??`（教えていないことだけは教える・第三節）。</summary>
        Unreadable = 1,

        /// <summary>空か当たりかは分かる。中身が何かまでは分からない。</summary>
        HitOrMiss = 2,

        /// <summary>当たりなら中身の品名まで見える。上機嫌でのみ働く最大精度（第六節）。</summary>
        Detailed = 3,
    }

    /// <summary>
    /// コツ `あたりの見分け方` の精度は SAN に依存する（第六節：コツが情報を与え、絶望が情報を奪う）。
    ///
    /// ゴミ箱は次の1回分を先に引いて確定させておく（再抽選しない＝嘘をつかない・第三節）。ここが決めるのは、
    /// その確定済みの結果をプレイヤーにどこまで開示するかだけである。開示の段階は食品カードの `??`（第三節）と
    /// 同じリズムを刻む：上機嫌で最大精度、崩壊帯では読めない。低 SAN では読めない＝賭けが保存される。
    /// </summary>
    public static class ForageSight
    {
        public static ForagePeek Read(bool hasSpotDuds, float sanity)
        {
            if (!hasSpotDuds)
            {
                return ForagePeek.Hidden;
            }

            if (sanity < SanityScale.BreakdownThreshold)
            {
                return ForagePeek.Unreadable;
            }

            return sanity >= SanityScale.ElatedThreshold ? ForagePeek.Detailed : ForagePeek.HitOrMiss;
        }
    }
}
