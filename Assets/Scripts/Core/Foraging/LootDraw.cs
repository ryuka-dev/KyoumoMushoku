using KyoumoMushoku.Core.Items;

namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// ゴミ箱を1回漁った結果。空振り（第六節・第十節）か、1個体の産出か。
    /// 食品なら状態も確定している（第十一節）。状態が読めるかどうかは SAN の問題であって、
    /// 抽選そのものは常に確定している。情報は奪ってよいが、結果を偽ってはならない（第三節）。
    /// </summary>
    public readonly struct LootDraw
    {
        LootDraw(bool hasItem, ItemId item, FoodState state)
        {
            HasItem = hasItem;
            Item = item;
            State = state;
        }

        /// <summary>何か出たか。false なら空振り。</summary>
        public bool HasItem { get; }

        public ItemId Item { get; }

        /// <summary>食品なら状態。食品以外では意味を持たない。</summary>
        public FoodState State { get; }

        /// <summary>空振り。</summary>
        public static readonly LootDraw Empty = new LootDraw(false, default, FoodState.Fresh);

        public static LootDraw Of(ItemId item, FoodState state) => new LootDraw(true, item, state);
    }
}
