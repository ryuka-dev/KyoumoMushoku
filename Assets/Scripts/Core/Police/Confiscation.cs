using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 没収の規則（第五節）。捕まったときと、行き倒れて病院に運ばれたとき（第三節）の両方がこれを使う。
    /// 同じ「取り上げられる」という出来事に、2つの規則を持たせない。
    ///
    /// 第五節は「違法品・腐敗品の一部を没収」と書くが、初版の全15種（第十一節）に違法品は存在しない。
    /// したがって現時点で没収できるのは腐敗した食品だけである。分類で判定しているため、
    /// 違法品の分類が増えたときは <see cref="IsSeizable"/> に条件を1つ足せばよい。
    /// </summary>
    public static class Confiscation
    {
        /// <summary>取り上げられるのは腐敗した食品のみ。新鮮な食品・傷んだ食品・換金廃品には手を付けない。</summary>
        public static bool IsSeizable(ItemDefinition definition, FoodState freshness) =>
            definition != null && definition.IsFood && freshness == FoodState.Rotten;

        /// <summary>
        /// 没収される個体の索引を選ぶ。「一部」であって全部ではない（第五節）。
        /// 対象のうち半数（端数切り上げ）を無作為に選ぶ。
        /// </summary>
        /// <returns>
        /// 降順に並んだ索引。呼び出し側はこの順に <c>TryRemoveAt</c> を呼べば、
        /// 除去による索引のずれを気にしなくてよい。
        /// </returns>
        public static IReadOnlyList<int> SelectSeized(
            IReadOnlyList<ItemInstance> items, IItemCatalog catalog, IRng rng)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (catalog is null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var candidates = new List<int>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (catalog.TryGet(item.ItemId, out var definition) && IsSeizable(definition, item.Freshness))
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return Array.Empty<int>();
            }

            var takeCount = (candidates.Count + 1) / 2;

            // 候補から無作為に takeCount 個を抜く。抜いた分だけ候補が縮む。
            var seized = new List<int>(takeCount);
            for (var taken = 0; taken < takeCount; taken++)
            {
                var pick = (int)(rng.NextDouble() * candidates.Count);
                if (pick >= candidates.Count)
                {
                    pick = candidates.Count - 1;
                }

                seized.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            seized.Sort();
            seized.Reverse();
            return seized;
        }
    }
}
