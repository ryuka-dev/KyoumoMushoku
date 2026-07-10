using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>飲食・使用の結果。</summary>
    public enum ConsumeResult
    {
        Consumed = 0,

        /// <summary>その場所にアイテムが無い。</summary>
        NoItem = 1,

        /// <summary>飲食・使用できない分類（換金廃品・装備）。</summary>
        NotConsumable = 2,
    }

    /// <summary>
    /// カバンの中のひとつを口に入れる。空腹・渇き・SAN・HP への効果を適用し、消費したら取り除く。
    ///
    /// 腐敗のみが追加の代償を持つ。空腹の回復量は状態に依存しない（第十一節）。
    /// 「読めるかどうか」（`??`）は表示の問題であり、実際の効果は常に確定している。
    /// 情報は奪ってよいが、結果そのものを偽ってはならない。
    /// </summary>
    public static class Consumption
    {
        public static ConsumeResult TryConsume(Inventory inventory, int index, Vitals vitals, out ItemDefinition consumed)
        {
            consumed = null;

            if (inventory is null || vitals is null)
            {
                return ConsumeResult.NoItem;
            }

            if (!inventory.TryGetDefinition(index, out var definition))
            {
                return ConsumeResult.NoItem;
            }

            if (!definition.IsConsumable)
            {
                return ConsumeResult.NotConsumable;
            }

            var freshness = inventory[index].Freshness;
            var effect = definition.IsFood ? definition.EffectFor(freshness) : definition.Effect;

            vitals.Apply(effect);
            inventory.TryRemoveAt(index, out _);
            consumed = definition;
            return ConsumeResult.Consumed;
        }
    }
}
