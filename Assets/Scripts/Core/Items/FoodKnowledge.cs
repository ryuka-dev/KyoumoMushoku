using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>食品の状態がプレイヤーにどこまで読めるか。</summary>
    public enum FoodStateReading
    {
        /// <summary>状態が正確に分かる。結果（HP・SAN）も読める。</summary>
        Exact = 0,

        /// <summary>「新鮮ではない」ことだけ分かる。傷みか腐敗かは分からない。</summary>
        NotFresh = 1,

        /// <summary>何も分からない。`??`。</summary>
        Unknown = 2,
    }

    /// <summary>
    /// 情報を奪ってよいが、嘘をついてはならない（第三節）。
    /// 誤った数値を真実であるかのように見せる代わりに、欠落（`??`）を用いる。
    /// 教えない。ただし「教えていない」ということだけは必ず教える。
    /// </summary>
    public static class FoodKnowledge
    {
        /// <summary>SAN に応じて、食品の状態がどこまで読めるかを決める。</summary>
        public static FoodStateReading Read(FoodState state, float sanity)
        {
            if (sanity >= SanityScale.ClearSightThreshold)
            {
                return FoodStateReading.Exact;
            }

            if (sanity < SanityScale.BreakdownThreshold)
            {
                return FoodStateReading.Unknown;
            }

            // 傷みと腐敗の区別はつかないが、新鮮かどうかは分かる。
            return state == FoodState.Fresh ? FoodStateReading.Exact : FoodStateReading.NotFresh;
        }

        /// <summary>
        /// 食べた結果（HP・SAN の増減）を読めるか。
        /// 状態に依存しない行（空腹の回復量）はいつでも見える。必ず腹は膨れる。
        /// 何を失うかだけが分からない。
        /// </summary>
        public static bool KnowsConsequences(FoodStateReading reading) => reading == FoodStateReading.Exact;

        /// <summary>腐敗した食品だけが危険である。傷みは新鮮と同じ結果しかもたらさない。</summary>
        public static bool IsDangerous(FoodState state) => state == FoodState.Rotten;
    }
}
