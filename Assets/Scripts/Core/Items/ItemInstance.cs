using System;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// インベントリに入っているアイテム1個体。セーブデータにそのまま載る形である。
    /// 識別子を文字列で持つのは永続化のためであり、比較には <see cref="ItemId"/> を用いる。
    /// </summary>
    [Serializable]
    public struct ItemInstance
    {
        public string Id;

        /// <summary>食品の状態。食品以外では意味を持たない。入手した瞬間に確定する。</summary>
        public FoodState Freshness;

        public ItemInstance(ItemId id, FoodState freshness = FoodState.Fresh)
        {
            Id = id.Value;
            Freshness = freshness;
        }

        public ItemId ItemId => new ItemId(Id);
    }
}
