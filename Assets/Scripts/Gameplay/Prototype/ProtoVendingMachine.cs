using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 自販機。店に入らずに水が買える（値段は品のデータ＝店と同じ）。精神崩壊すると値札が読めない（第三節）。
    /// 下に落ちた小銭は <see cref="ProtoCoinSearch"/> が持つ。
    /// </summary>
    public sealed class ProtoVendingMachine : MonoBehaviour, IProtoUsable
    {
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] string _itemId = "water_bottle";

        public void Configure(ItemDatabaseAsset catalog, string itemId)
        {
            _catalog = catalog;
            _itemId = itemId;
        }

        public string Prompt(ProtoHands hands)
        {
            if (_catalog == null || !_catalog.TryGet(new ItemId(_itemId), out var definition))
            {
                return null;
            }

            var sanity = hands.Vitals != null && hands.Vitals.Vitals != null ? hands.Vitals.Vitals.Sanity : 100f;
            var price = SanityScale.CanReadPrices(sanity)
                ? ProtoText.Price(definition.BuyPriceYen)
                : ShopText.OfferPriceUnreadable;
            return ProtoText.VendingPrompt(definition.DisplayName, price);
        }

        public string Use(ProtoHands hands)
        {
            var result = StorePurchase.TryBuy(hands.Wallet, hands.Bag, _catalog, new ItemId(_itemId), out var bought);
            switch (result)
            {
                case PurchaseResult.Bought:
                    return ProtoText.VendingBought(bought.DisplayName);
                case PurchaseResult.CannotAfford:
                    return ProtoText.VendingCannotAfford;
                case PurchaseResult.InventoryFull:
                    return ProtoText.VendingBagFull;
                default:
                    return null;
            }
        }
    }
}
