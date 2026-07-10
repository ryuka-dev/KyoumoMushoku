using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;

namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// 食品の状態（新鮮／傷み／腐敗）の抽選確率。ゴミ箱の種類・時間帯ごとに異なる（第十一節・第十三節）。
    /// 状態は入手した瞬間に確定し、その後は一切変化しない。
    ///
    /// 重みは叩き台であり、プレイテストで調整する。
    /// </summary>
    public readonly struct FoodStateOdds
    {
        readonly float _fresh;
        readonly float _stale;
        readonly float _rotten;

        public FoodStateOdds(float fresh, float stale, float rotten)
        {
            _fresh = NonNegative(fresh);
            _stale = NonNegative(stale);
            _rotten = NonNegative(rotten);
        }

        public float Total => _fresh + _stale + _rotten;

        /// <summary>
        /// 状態を1つ抽選する。すべての重みが 0 のときは安全側の <see cref="FoodState.Stale"/> を返す
        /// （危険な腐敗を既定にしない。傷みは危険ではなく、読めないことだけが危険である）。
        /// </summary>
        public FoodState Roll(IRng rng)
        {
            var total = Total;
            if (total <= 0f)
            {
                return FoodState.Stale;
            }

            var u = (float)rng.NextDouble() * total;
            if (u < _fresh)
            {
                return FoodState.Fresh;
            }

            return u < _fresh + _stale ? FoodState.Stale : FoodState.Rotten;
        }

        static float NonNegative(float value) => value < 0f ? 0f : value;
    }
}
