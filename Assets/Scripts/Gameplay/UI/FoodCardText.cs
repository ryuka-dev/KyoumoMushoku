using System.Text;
using KyoumoMushoku.Core.Items;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 食品カードの表示文字列を組み立てる（第十一節）。
    ///
    /// 状態に依存しない行（空腹の回復量）はそのまま見え、状態に依存する行（HP・SAN）だけが
    /// `??` になる。必ず腹は膨れる。何を失うかだけが分からない。
    /// `??` の脇には必ず理由を添え、「見えていない」と「作られていない」を区別できるようにする。
    /// </summary>
    public static class FoodCardText
    {
        public const string UnreadableReason = "「よく見えない」";

        /// <summary>1つのアイテムの見出し行（名称＋状態）。</summary>
        public static string Headline(ItemDefinition definition, FoodState state, float sanity)
        {
            if (!definition.IsFood)
            {
                return definition.DisplayName;
            }

            var reading = FoodKnowledge.Read(state, sanity);
            var suffix = reading switch
            {
                FoodStateReading.Exact => ExactSuffix(state),
                FoodStateReading.NotFresh => "（新鮮ではない）",
                _ => "（??）",
            };

            return definition.DisplayName + suffix;
        }

        /// <summary>効果の複数行。食品は空腹を常に見せ、危険（HP・SAN）は SAN に応じて隠す。</summary>
        public static string EffectLines(ItemDefinition definition, FoodState state, float sanity)
        {
            var sb = new StringBuilder();

            if (definition.IsFood)
            {
                AppendFoodEffect(sb, definition, state, sanity);
                return sb.ToString();
            }

            // 食品以外（水・消耗品・換金廃品）は状態を持たず、効果はそのまま見える。
            AppendSigned(sb, "喉", definition.Effect.Thirst);
            AppendSigned(sb, "空腹", definition.Effect.Hunger);
            AppendSigned(sb, "気分", definition.Effect.Sanity);
            AppendSigned(sb, "HP", definition.Effect.Hp);

            if (definition.Category == ItemCategory.Salvage && definition.SellPriceYen > 0)
            {
                sb.AppendLine($"売値 {definition.SellPriceYen}円");
            }

            return sb.ToString();
        }

        static void AppendFoodEffect(StringBuilder sb, ItemDefinition definition, FoodState state, float sanity)
        {
            // 空腹の回復量は状態に依存しない。常に見える。
            AppendSigned(sb, "空腹", definition.Effect.Hunger);

            var reading = FoodKnowledge.Read(state, sanity);
            if (FoodKnowledge.KnowsConsequences(reading))
            {
                var effect = definition.EffectFor(state);

                // 新鮮・傷みでは危険はない。腐敗のときだけ HP・SAN の代償が見える。
                if (effect.Hp != definition.Effect.Hp)
                {
                    AppendSigned(sb, "HP", effect.Hp);
                }

                if (effect.Sanity != definition.Effect.Sanity)
                {
                    AppendSigned(sb, "気分", effect.Sanity);
                }

                return;
            }

            // 読めないときは、危険の有無だけを伏せる。腹が膨れることは既に見せている。
            sb.AppendLine("HP    ??");
            sb.AppendLine("気分  ??");
            sb.AppendLine(UnreadableReason);
        }

        static string ExactSuffix(FoodState state) => state switch
        {
            FoodState.Rotten => "（腐敗）",
            FoodState.Stale => "（傷み）",
            _ => "（新鮮）",
        };

        static void AppendSigned(StringBuilder sb, string label, float value)
        {
            if (value == 0f)
            {
                return;
            }

            var sign = value > 0f ? "+" : "";
            sb.AppendLine($"{label}  {sign}{value:0}");
        }
    }
}
