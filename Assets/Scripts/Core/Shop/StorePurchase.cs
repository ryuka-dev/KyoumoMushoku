using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;

namespace KyoumoMushoku.Core.Shop
{
    /// <summary>購入の結果。使えない理由は世界の言葉で店主が語る（第十四節）。</summary>
    public enum PurchaseResult
    {
        Bought = 0,

        /// <summary>所持金が足りない。</summary>
        CannotAfford = 1,

        /// <summary>カバンに入りきらない。金は取らない（漁りと同じ正直さ）。</summary>
        InventoryFull = 2,

        /// <summary>買い切りの装備（バックパック）を既に持っている。二度は売らない。</summary>
        AlreadyOwned = 3,

        /// <summary>その品は店に無い。</summary>
        Unavailable = 4,
    }

    /// <summary>
    /// 店で買う（第九・十一節）。金と持ち物の両方に触れる取引をひとつの場所に閉じる。
    ///
    /// 店で買った食品は常に新鮮である（第十一節）。これは絶望の悪循環に対する逃げ道でもある。
    /// 金が買っているのは確実性であり、ゴミに賭ける必要がなくなる（第三節）。
    ///
    /// 入りきらないときは金を取らない。漁りが「入りきらないときは資源を消費しない」のと同じ規則を、
    /// 支払いにも適用する。取ってから入らないと言われるのは、静かな損失であり許さない（第十一節）。
    /// </summary>
    public static class StorePurchase
    {
        public static PurchaseResult TryBuy(
            Wallet wallet, Inventory inventory, IItemCatalog catalog, ItemId id, out ItemDefinition bought)
        {
            bought = null;

            if (wallet is null || inventory is null || catalog is null)
            {
                return PurchaseResult.Unavailable;
            }

            if (!catalog.TryGet(id, out var definition) || !definition.IsForSale)
            {
                return PurchaseResult.Unavailable;
            }

            // バックパックは持ち物に入らず、容量を広げる買い切りである（第十一節）。
            if (definition.CapacityBonus > 0)
            {
                var target = Inventory.DefaultCapacity + definition.CapacityBonus;
                if (inventory.Capacity >= target)
                {
                    return PurchaseResult.AlreadyOwned;
                }

                if (!wallet.CanAfford(definition.BuyPriceYen))
                {
                    return PurchaseResult.CannotAfford;
                }

                wallet.TrySpend(definition.BuyPriceYen);
                inventory.Expand(target - inventory.Capacity);
                bought = definition;
                return PurchaseResult.Bought;
            }

            // 店の食品は常に新鮮。
            var instance = new ItemInstance(id, FoodState.Fresh);

            if (!inventory.CanAdd(instance))
            {
                return PurchaseResult.InventoryFull;
            }

            if (!wallet.CanAfford(definition.BuyPriceYen))
            {
                return PurchaseResult.CannotAfford;
            }

            wallet.TrySpend(definition.BuyPriceYen);
            inventory.TryAdd(instance);
            bought = definition;
            return PurchaseResult.Bought;
        }
    }
}
