using System;
using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// アイテムの不変の定義。実行時の1個体は <see cref="ItemInstance"/> が表す。
    /// 占有マス数は叩き台であり、プレイテストで調整する（第十一節）。
    /// </summary>
    public sealed class ItemDefinition
    {
        public ItemDefinition(
            ItemId id,
            string displayName,
            ItemCategory category,
            int slots,
            VitalsDelta effect = default,
            VitalsDelta rottenPenalty = default,
            int sellPriceYen = 0)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("アイテムの識別子が空である。", nameof(id));
            }

            if (slots < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slots), slots, "占有マス数は1以上。");
            }

            Id = id;
            DisplayName = displayName ?? id.Value;
            Category = category;
            Slots = slots;
            Effect = effect;
            RottenPenalty = rottenPenalty;
            SellPriceYen = sellPriceYen;
        }

        public ItemId Id { get; }

        /// <summary>表示名。識別子として使ってはならない。</summary>
        public string DisplayName { get; }

        public ItemCategory Category { get; }

        /// <summary>インベントリで占有するマス数。</summary>
        public int Slots { get; }

        /// <summary>状態に依存しない効果。食品なら空腹の回復量がここに入る。</summary>
        public VitalsDelta Effect { get; }

        /// <summary>腐敗しているときにのみ、<see cref="Effect"/> に加えて適用される代償。</summary>
        public VitalsDelta RottenPenalty { get; }

        public int SellPriceYen { get; }

        public bool IsFood => Category == ItemCategory.Food;

        /// <summary>飲食・使用できるか。</summary>
        public bool IsConsumable =>
            Category == ItemCategory.Food ||
            Category == ItemCategory.Water ||
            Category == ItemCategory.Consumable;

        /// <summary>
        /// 実際に口に入れたときの効果。腐敗のみが追加の代償を持つ。
        /// 傷みは新鮮と同じ結果しかもたらさない。読めないことだけが危険である。
        /// </summary>
        public VitalsDelta EffectFor(FoodState state)
        {
            if (!IsFood || state != FoodState.Rotten)
            {
                return Effect;
            }

            return Effect + RottenPenalty;
        }
    }
}
