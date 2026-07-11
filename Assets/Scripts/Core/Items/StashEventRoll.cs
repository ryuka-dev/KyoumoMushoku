using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 保管庫イベントの抽選（第十二節）。乱数源を境界の内側に留め、決定的に検証できるようにする。
    ///
    /// 発生確率は警戒度と保管量の両方から作る（どちらか一方だけでは高くならない）。発生したら、
    /// 種別を主因の重みで引く（清掃∝警戒度／同業者∝量／撤去∝高警戒度）。空の保管庫は脅かされない。
    /// </summary>
    public static class StashEventRoll
    {
        /// <summary>
        /// その日に起きるイベントを引く。起きないなら <see cref="StashEventKind.None"/>。
        /// <paramref name="safetyMultiplier"/>（[0,1]・既定 1）は保管庫の安全性で、発生確率を下げる
        /// （場所代・種別。<see cref="StashSafety"/>）。1 は無防備、0 は完全に安全。
        /// </summary>
        public static StashEventKind Roll(
            IRng rng, float residentialAlert, int usedSlots, float safetyMultiplier = 1f)
        {
            if (rng is null || usedSlots <= 0)
            {
                return StashEventKind.None;
            }

            var alert01 = Clamp01(residentialAlert / ZoneAlertTuning.MaxLevel);
            var fullness01 = Clamp01(usedSlots / (float)StashEventTuning.FullnessReferenceSlots);
            var probability = (alert01 * StashEventTuning.AlertContribution +
                               fullness01 * StashEventTuning.FullnessContribution) * Clamp01(safetyMultiplier);

            if (rng.NextDouble() >= probability)
            {
                return StashEventKind.None;
            }

            // 主因の重みで種別を引く。清掃は警戒度、同業者は量、撤去は高警戒度に比例する。
            var weightCleaning = residentialAlert;
            var weightPeers = usedSlots * 8f;
            var weightPolice = residentialAlert >= StashEventTuning.PoliceRemovalAlertFloor
                ? (residentialAlert - StashEventTuning.PoliceRemovalAlertFloor) * 2f
                : 0f;

            var total = weightCleaning + weightPeers + weightPolice;
            if (total <= 0f)
            {
                return StashEventKind.None;
            }

            var pick = rng.NextDouble() * total;
            if (pick < weightCleaning)
            {
                return StashEventKind.CityCleaning;
            }

            return pick < weightCleaning + weightPeers
                ? StashEventKind.ScavengedByPeers
                : StashEventKind.PoliceRemoval;
        }

        /// <summary>種別と保管点数から、失う点数を求める（切り上げ・在庫を超えない）。</summary>
        public static int LostCount(int itemCount, StashEventKind kind)
        {
            if (itemCount <= 0 || kind == StashEventKind.None)
            {
                return 0;
            }

            var lost = (int)Math.Ceiling(itemCount * (double)StashEventTuning.LossFractionFor(kind));
            if (lost < 0)
            {
                return 0;
            }

            return lost > itemCount ? itemCount : lost;
        }

        /// <summary>
        /// 失う点の索引を選ぶ。降順で返すので、この順に抜けば索引がずれない。
        /// どの点を失うかは無作為（値の高低で狙い撃ちしない）。
        /// </summary>
        public static IReadOnlyList<int> SelectLost(int itemCount, StashEventKind kind, IRng rng)
        {
            var count = LostCount(itemCount, kind);
            var result = new List<int>(count > 0 ? count : 0);
            if (count <= 0 || rng is null)
            {
                return result;
            }

            var indices = new int[itemCount];
            for (var i = 0; i < itemCount; i++)
            {
                indices[i] = i;
            }

            // 部分的なフィッシャー–イェーツ。先頭 count 個が無作為に選ばれた索引になる。
            for (var i = 0; i < count; i++)
            {
                var j = i + (int)(rng.NextDouble() * (itemCount - i));
                if (j >= itemCount)
                {
                    j = itemCount - 1;
                }

                (indices[i], indices[j]) = (indices[j], indices[i]);
                result.Add(indices[i]);
            }

            result.Sort((a, b) => b.CompareTo(a));
            return result;
        }

        static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);
    }
}
