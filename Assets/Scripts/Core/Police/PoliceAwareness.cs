using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>警官の目（こちらへの注目）に、プレイヤーがどこまで気づけるか。</summary>
    public enum PoliceAwareness
    {
        /// <summary>気づけない。見られていても、目の印は出ない。</summary>
        None = 0,

        /// <summary>目の前の警官の目にだけ気づく。視界の外（背後・横）の警官の目には気づけない。</summary>
        InFrontOnly = 1,

        /// <summary>視界の外の警官の目にも、気配で気づく（画面の端に方向を示す）。</summary>
        Full = 2,
    }

    /// <summary>
    /// SAN＝都市を読む力（第三節）を、警官の目に気づく力に当てる。注目度そのものは SAN に関係なく上がり、
    /// ここが決めるのは、それをどこまで見せるかだけである（情報を奪ってよいが、嘘はつかない）。
    /// 警官の声と足音は世界の事実なので、SAN が低くても聞こえる。段の境は叩いた手応えの読み（<c>HitTellSight</c>）と同じ。
    /// </summary>
    public static class PoliceAwarenessSight
    {
        public static PoliceAwareness Read(float sanity)
        {
            if (sanity < SanityScale.BreakdownThreshold)
            {
                return PoliceAwareness.None;
            }

            return sanity < SanityScale.ClearSightThreshold ? PoliceAwareness.InFrontOnly : PoliceAwareness.Full;
        }

        /// <summary>警官の目の印を出すか。<paramref name="onScreen"/> は警官が視界の中にいるか。</summary>
        public static bool Shows(PoliceAwareness awareness, bool onScreen) =>
            awareness == PoliceAwareness.Full || (awareness == PoliceAwareness.InFrontOnly && onScreen);

        /// <summary>
        /// 警官がどちらを見ているか（視線の届く範囲）まで読めるか。上機嫌の報酬（第三節：報酬は情報だけで、数値は足さない）。
        /// 機嫌よく街を歩いている人には、巡査がどこを見ているかが分かる。
        /// </summary>
        public static bool ReadsGaze(float sanity) => sanity >= SanityScale.ElatedThreshold;
    }
}
