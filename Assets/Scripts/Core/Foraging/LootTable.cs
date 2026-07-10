using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;

namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// ゴミ箱1種・1時間帯ぶんの産出テーブル。重み付きのアイテム候補と「空振りの重み」を持ち、
    /// 1回の抽選で「空振り」か「1個体」を返す（第十節：期待感と空振り）。
    ///
    /// 食品として引かれたものだけ、続けて状態（新鮮／傷み／腐敗）を抽選する（第十一節）。
    /// どの候補が食品かは <see cref="IItemCatalog"/> に問い合わせるため、テーブル側は分類を二重に持たない。
    ///
    /// 重みはすべて叩き台であり、プレイテストで調整する。
    /// </summary>
    public sealed class LootTable
    {
        public readonly struct Entry
        {
            public Entry(ItemId item, float weight)
            {
                Item = item;
                Weight = weight < 0f ? 0f : weight;
            }

            public ItemId Item { get; }
            public float Weight { get; }
        }

        readonly Entry[] _entries;
        readonly float _emptyWeight;
        readonly FoodStateOdds _foodOdds;

        public LootTable(IEnumerable<Entry> entries, float emptyWeight, FoodStateOdds foodOdds)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries = new List<Entry>(entries).ToArray();
            _emptyWeight = emptyWeight < 0f ? 0f : emptyWeight;
            _foodOdds = foodOdds;
        }

        /// <summary>
        /// 1回ぶんの抽選。まず空振りかどうかを決め、当たりなら候補を1つ選ぶ。
        /// 選ばれたものが食品なら、続けて状態を抽選する。
        /// </summary>
        public LootDraw Draw(IRng rng, IItemCatalog catalog)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var total = _emptyWeight;
            foreach (var entry in _entries)
            {
                total += entry.Weight;
            }

            if (total <= 0f)
            {
                return LootDraw.Empty;
            }

            var u = (float)rng.NextDouble() * total;
            if (u < _emptyWeight)
            {
                return LootDraw.Empty;
            }

            u -= _emptyWeight;
            foreach (var entry in _entries)
            {
                if (u < entry.Weight)
                {
                    return LootDraw.Of(entry.Item, RollState(entry.Item, rng, catalog));
                }

                u -= entry.Weight;
            }

            // 浮動小数の誤差で最後の候補を取りこぼした場合の保険。
            return LootDraw.Empty;
        }

        FoodState RollState(ItemId item, IRng rng, IItemCatalog catalog)
        {
            if (catalog != null && catalog.TryGet(item, out var definition) && definition.IsFood)
            {
                return _foodOdds.Roll(rng);
            }

            // 食品以外は状態を持たない。
            return FoodState.Fresh;
        }
    }
}
