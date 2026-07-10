namespace KyoumoMushoku.Core.Items
{
    /// <summary>アイテムの分類（第十一節）。</summary>
    public enum ItemCategory
    {
        /// <summary>水。渇きを回復する。</summary>
        Water = 0,

        /// <summary>食品。空腹を回復する。状態（新鮮／傷み／腐敗）を持つ唯一の分類。</summary>
        Food = 1,

        /// <summary>換金廃品。売却して現金化する。飲食はできない。</summary>
        Salvage = 2,

        /// <summary>装備。買い切りの非消耗品。</summary>
        Equipment = 3,

        /// <summary>消耗品。一度きりの効果を持つ。</summary>
        Consumable = 4,

        /// <summary>特殊・進行アイテム。</summary>
        Special = 5,
    }
}
