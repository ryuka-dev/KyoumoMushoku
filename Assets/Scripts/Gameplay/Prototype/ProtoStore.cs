using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Shop;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 試作のコンビニ。廃品の買い取りとバックパックの販売だけを持つ（作業計画 第一の二節「鞄」）。
    /// 鞄が一杯になったら売りに行く → 金 → バックパックで容量が増える、という輪を一人称で確かめる。
    ///
    /// 取引の規則は本編と同じ Core（<see cref="SalvageBuyback"/>・<see cref="StorePurchase"/>）と
    /// <c>Assets/Config/ShopTuning.asset</c> の買い取り上限を使う。店主の台詞も本編の <see cref="ShopText"/> を使う。
    /// この店が持つのは、その日の買い取り額の帳簿（<see cref="SalvageLedger"/>）だけ。金と鞄はプレイヤー（<see cref="ProtoHands"/>）が持つ。
    /// 本編の就寝は無いので、昼夜の切り替えで夜から昼になったときを新しい一日とする。
    /// </summary>
    public sealed class ProtoStore : MonoBehaviour
    {
        [SerializeField] ShopTuningAsset _tuning;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] string _backpackId = "backpack";

        readonly SalvageLedger _ledger = new SalvageLedger();

        public int RemainingToday => _ledger.RemainingToday(DailyCap);

        int DailyCap => _tuning != null ? _tuning.BuybackDailyCapYen : 0;

        public void Configure(ShopTuningAsset tuning, ItemDatabaseAsset catalog, ProtoDayNight dayNight)
        {
            _tuning = tuning;
            _catalog = catalog;
            _dayNight = dayNight;
        }

        void OnEnable()
        {
            if (_dayNight != null)
            {
                _dayNight.Changed += OnDayNightChanged;
            }
        }

        void OnDisable()
        {
            if (_dayNight != null)
            {
                _dayNight.Changed -= OnDayNightChanged;
            }
        }

        void OnDayNightChanged()
        {
            if (!_dayNight.IsNight)
            {
                _ledger.BeginNextDay();
            }
        }

        /// <summary>鞄の換金廃品を、今日の上限まで売る。返すのは店主の一言。</summary>
        public string Sell(Inventory bag, Wallet wallet)
        {
            if (!HasSalvage(bag))
            {
                return ShopText.NothingToSell;
            }

            if (RemainingToday <= 0)
            {
                return ShopText.BuybackCapReached;
            }

            var result = SalvageBuyback.SellSalvage(wallet, bag, _ledger, DailyCap);
            return result.SoldCount > 0
                ? ShopText.SalvageSold(result.SoldCount, result.PaidYen, result.CapReached)
                : ShopText.BuybackCapReached;
        }

        /// <summary>バックパックを買う。返すのは店主の一言。</summary>
        public string BuyBackpack(Inventory bag, Wallet wallet)
        {
            var result = StorePurchase.TryBuy(wallet, bag, _catalog, new ItemId(_backpackId), out var bought);
            switch (result)
            {
                case PurchaseResult.Bought:
                    return ShopText.BoughtThanks(bought.DisplayName);
                case PurchaseResult.CannotAfford:
                    return ShopText.CannotAfford;
                case PurchaseResult.AlreadyOwned:
                    return ShopText.AlreadyOwned;
                case PurchaseResult.InventoryFull:
                    return ShopText.InventoryFull;
                default:
                    return ShopText.NotSold;
            }
        }

        /// <summary>照準を合わせたときの案内。値段は品のデータから出す。</summary>
        public string BackpackPrompt(Inventory bag)
        {
            if (_catalog == null || !_catalog.TryGet(new ItemId(_backpackId), out var definition))
            {
                return null;
            }

            var owned = bag != null && bag.Capacity >= Inventory.DefaultCapacity + definition.CapacityBonus;
            return owned
                ? ProtoText.BackpackOwned(definition.DisplayName)
                : ProtoText.BuyPrompt(definition.DisplayName, definition.BuyPriceYen, definition.CapacityBonus);
        }

        public string SellPrompt(Inventory bag) => ProtoText.SellPrompt(SalvageValue(bag), RemainingToday);

        int SalvageValue(Inventory bag)
        {
            var total = 0;
            if (bag == null)
            {
                return total;
            }

            for (var i = 0; i < bag.Count; i++)
            {
                if (bag.TryGetDefinition(i, out var definition) && definition.Category == ItemCategory.Salvage)
                {
                    total += definition.SellPriceYen;
                }
            }

            return total;
        }

        static bool HasSalvage(Inventory bag)
        {
            if (bag == null)
            {
                return false;
            }

            for (var i = 0; i < bag.Count; i++)
            {
                if (bag.TryGetDefinition(i, out var definition)
                    && definition.Category == ItemCategory.Salvage && definition.SellPriceYen > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
