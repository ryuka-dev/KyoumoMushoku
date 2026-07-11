using System;
using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Shop
{
    /// <summary>
    /// バイト（レジ打ち）の報酬を決める（第四・九・十一節）。
    ///
    /// バイトは安定した金銭を、比較的低いリスクで与える。ただし最も回復させづらい資源（SAN）を大量に消費する。
    /// ここで決めるのは金額だけであり、SAN・空腹・時間の消費は呼び出し側が適用する。
    ///
    /// 報酬は「出来（ミニゲームの命中率）× SAN 効率 × 基本額」である。SAN が崩壊帯（20 未満）に落ちると
    /// 効率が暴落する（<see cref="SanityScale.JobEfficiency"/>）。禁止ではない。「割に合わないが、まだできる」を残す（第三節）。
    ///
    /// これがひとつの閉じた輪を生む。SAN を削り続ける仕事は、やがて自分に対して割に合わなくなる。
    /// </summary>
    public static class JobReward
    {
        /// <summary>出来が満点・SAN が健全なときの報酬（第十一節）。</summary>
        public const int BaseRewardYen = 800;

        public static int Payout(float performance01, float sanity)
        {
            var performance = performance01 < 0f ? 0f : (performance01 > 1f ? 1f : performance01);
            var efficiency = SanityScale.JobEfficiency(sanity);
            return (int)Math.Round(BaseRewardYen * performance * efficiency, MidpointRounding.AwayFromZero);
        }
    }
}
