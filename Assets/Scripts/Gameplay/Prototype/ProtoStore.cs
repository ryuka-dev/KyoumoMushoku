using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Shop;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 試作のコンビニ。廃品の買い取りと、棚の品（水・食べ物・バックパック）の販売と、バイト（レジ打ち）の精算を持つ
    /// （作業計画 第一の二節「鞄」、一人称試作 試遊・一局目の次の手 2）。
    /// 鞄が一杯になったら売りに行く → 金 → バックパックで容量が増える、という輪を一人称で確かめる。
    ///
    /// 取引の規則は本編と同じ Core（<see cref="SalvageBuyback"/>・<see cref="StorePurchase"/>・<see cref="JobReward"/>）と
    /// <c>Assets/Config/ShopTuning.asset</c> の買い取り上限・バイトの数値を使う（2D 版と共有。書き換えない）。店主の台詞も本編の <see cref="ShopText"/> を使う。
    /// この店が持つのは、その日の買い取り額の帳簿（<see cref="SalvageLedger"/>）だけ。金と鞄はプレイヤー（<see cref="ProtoHands"/>）が持つ。
    /// 帳簿は就寝＝日付の変わり目で新しい一日になる（<see cref="BeginNextDay"/>。<see cref="ProtoSession"/> が呼ぶ）。
    /// </summary>
    public sealed class ProtoStore : MonoBehaviour
    {
        [SerializeField] ShopTuningAsset _tuning;
        [SerializeField] ItemDatabaseAsset _catalog;
        [Tooltip("バイト1シフトで進める時計。")]
        [SerializeField] ProtoDayNight _dayNight;

        readonly SalvageLedger _ledger = new SalvageLedger();

        public int RemainingToday => _ledger.RemainingToday(DailyCap);

        int DailyCap => _tuning != null ? _tuning.BuybackDailyCapYen : 0;

        /// <summary>レジ打ち1シフトのラウンド数。</summary>
        public int JobRounds => _tuning != null ? _tuning.JobRounds : 0;

        public void Configure(ShopTuningAsset tuning, ItemDatabaseAsset catalog, ProtoDayNight dayNight)
        {
            _tuning = tuning;
            _catalog = catalog;
            _dayNight = dayNight;
        }

        /// <summary>新しい一日。店主の買い取り枠が戻る。就寝からのみ呼ぶ。</summary>
        public void BeginNextDay() => _ledger.BeginNextDay();

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

        /// <summary>
        /// バイト1シフトぶんの精算。規則は本編の Storefront.ApplyJobOutcome と同じ：報酬は出来（<paramref name="performance01"/>）と
        /// SAN で決まり（<see cref="JobReward"/>）、SAN・渇き・空腹・時間の消費は出来によらず必ず起きる（働けば削られる）。
        /// 店にいる間は時間が止まっているので、1シフトの時間は時計をまとまって進めて払う。途中でやめたらここに来ない＝何も消費しない。
        /// 返すのは店主の一言。
        /// </summary>
        public string Work(float performance01, Vitals vitals, Wallet wallet)
        {
            if (_tuning == null || vitals == null || wallet == null)
            {
                return null;
            }

            var paid = JobReward.Payout(performance01, vitals.Sanity);
            wallet.Add(paid);
            vitals.Apply(new VitalsDelta { Sanity = -_tuning.JobSanityCost });
            vitals.DrainTime(_tuning.JobShiftSeconds, _tuning.JobDrainMultiplier);
            if (_dayNight != null)
            {
                _dayNight.Clock?.Advance(_tuning.JobShiftSeconds);
            }

            return ShopText.JobDone(paid);
        }

        /// <summary>棚の品を一つ買う。返すのは店主の一言。</summary>
        public string Buy(string itemId, Inventory bag, Wallet wallet)
        {
            var result = StorePurchase.TryBuy(wallet, bag, _catalog, new ItemId(itemId), out var bought);
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

        /// <summary>
        /// 照準を合わせたときの案内。値段は品のデータから出す。精神崩壊すると値札が読めない（第三節：価格 ??）。
        /// 読めなくても買うことはできる。欠落は用いるが、操作は奪わない。
        /// </summary>
        public string BuyPrompt(string itemId, Inventory bag, float sanity)
        {
            if (_catalog == null || !_catalog.TryGet(new ItemId(itemId), out var definition))
            {
                return null;
            }

            var price = SanityScale.CanReadPrices(sanity)
                ? ProtoText.Price(definition.BuyPriceYen)
                : ShopText.OfferPriceUnreadable;
            if (definition.CapacityBonus <= 0)
            {
                return ProtoText.BuyPrompt(definition.DisplayName, price);
            }

            var owned = bag != null && bag.Capacity >= Inventory.DefaultCapacity + definition.CapacityBonus;
            return owned
                ? ProtoText.BackpackOwned(definition.DisplayName)
                : ProtoText.BuyBackpackPrompt(definition.DisplayName, price, definition.CapacityBonus);
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
