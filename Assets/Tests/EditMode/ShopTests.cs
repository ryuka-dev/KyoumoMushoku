using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    static class ShopItems
    {
        public static readonly ItemId Water = new ItemId("water_bottle");
        public static readonly ItemId Onigiri = new ItemId("onigiri");
        public static readonly ItemId Backpack = new ItemId("backpack");
        public static readonly ItemId Can = new ItemId("can_aluminum");
        public static readonly ItemId Umbrella = new ItemId("umbrella_broken");
        public static readonly ItemId Bread = new ItemId("bread");

        public static FakeCatalog Catalog() => new FakeCatalog()
            // 買値つき（店頭に並ぶ）。店の食品は常に新鮮。
            .Add(new ItemDefinition(Water, "ペットボトルの水", ItemCategory.Water, slots: 2,
                effect: new VitalsDelta { Thirst = 45f }, buyPriceYen: 100))
            .Add(new ItemDefinition(Onigiri, "おにぎり", ItemCategory.Food, slots: 1,
                effect: new VitalsDelta { Hunger = 25f }, buyPriceYen: 130))
            .Add(new ItemDefinition(Backpack, "バックパック", ItemCategory.Equipment, slots: 1,
                buyPriceYen: 2000, capacityBonus: 12))
            // 換金廃品（売値つき・店頭には並ばない）。
            .Add(new ItemDefinition(Can, "アルミ缶", ItemCategory.Salvage, slots: 1, sellPriceYen: 20))
            .Add(new ItemDefinition(Umbrella, "壊れた傘", ItemCategory.Salvage, slots: 3, sellPriceYen: 120))
            .Add(new ItemDefinition(Bread, "パン", ItemCategory.Food, slots: 2, effect: new VitalsDelta { Hunger = 22f }));
    }

    public sealed class StorePurchaseTests
    {
        [Test]
        public void BuyingFood_TakesMoneyAndYieldsAFreshItem()
        {
            var wallet = new Wallet(500);
            var inventory = new Inventory(ShopItems.Catalog());

            var result = StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Onigiri, out var bought);

            Assert.AreEqual(PurchaseResult.Bought, result);
            Assert.AreEqual(370, wallet.Yen);
            Assert.AreEqual(1, inventory.Count);
            Assert.AreEqual(FoodState.Fresh, inventory[0].Freshness, "店の食品は常に新鮮である（第十一節）。");
            Assert.AreEqual(ShopItems.Onigiri, bought.Id);
        }

        [Test]
        public void WhatIsNotForSale_IsRefused()
        {
            var wallet = new Wallet(500);
            var inventory = new Inventory(ShopItems.Catalog());

            // パンは売値も買値も持たない食品。店頭には並ばない。
            Assert.AreEqual(PurchaseResult.Unavailable,
                StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Bread, out _));
            Assert.AreEqual(500, wallet.Yen);
        }

        [Test]
        public void WithoutEnoughMoney_NothingIsBought()
        {
            var wallet = new Wallet(50);
            var inventory = new Inventory(ShopItems.Catalog());

            Assert.AreEqual(PurchaseResult.CannotAfford,
                StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Onigiri, out _));
            Assert.AreEqual(50, wallet.Yen);
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void WhenTheBagIsFull_NoMoneyIsTaken()
        {
            var wallet = new Wallet(500);
            var inventory = new Inventory(ShopItems.Catalog(), capacity: 1);
            // 1マスを埋める。ペットボトルの水は2マスなので入らない。
            inventory.TryAdd(new ItemInstance(ShopItems.Onigiri));

            var result = StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Water, out _);

            Assert.AreEqual(PurchaseResult.InventoryFull, result);
            Assert.AreEqual(500, wallet.Yen, "入りきらないときは金を取らない（漁りと同じ正直さ）。");
        }

        [Test]
        public void BuyingABackpack_WidensTheBagInsteadOfFillingIt()
        {
            var wallet = new Wallet(2500);
            var inventory = new Inventory(ShopItems.Catalog());
            Assert.AreEqual(Inventory.DefaultCapacity, inventory.Capacity);

            var result = StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Backpack, out _);

            Assert.AreEqual(PurchaseResult.Bought, result);
            Assert.AreEqual(500, wallet.Yen);
            Assert.AreEqual(Inventory.DefaultCapacity + 12, inventory.Capacity);
            Assert.AreEqual(0, inventory.Count, "バックパックは持ち物に入らない。容量そのものを広げる。");
        }

        [Test]
        public void ABackpack_IsNeverSoldTwice()
        {
            var wallet = new Wallet(5000);
            var inventory = new Inventory(ShopItems.Catalog());
            StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Backpack, out _);

            var result = StorePurchase.TryBuy(wallet, inventory, ShopItems.Catalog(), ShopItems.Backpack, out _);

            Assert.AreEqual(PurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(3000, wallet.Yen, "二度は買わされない。");
            Assert.AreEqual(Inventory.DefaultCapacity + 12, inventory.Capacity);
        }
    }

    public sealed class SalvageBuybackTests
    {
        [Test]
        public void SellingSalvage_PaysAndEmptiesOnlyTheSalvage()
        {
            var wallet = new Wallet(0);
            var inventory = new Inventory(ShopItems.Catalog(), capacity: 10);
            inventory.TryAdd(new ItemInstance(ShopItems.Can));
            inventory.TryAdd(new ItemInstance(ShopItems.Can));
            inventory.TryAdd(new ItemInstance(ShopItems.Onigiri)); // 食品は売らない
            var ledger = new SalvageLedger();

            var result = SalvageBuyback.SellSalvage(wallet, inventory, ledger, dailyCapYen: 300);

            Assert.AreEqual(2, result.SoldCount);
            Assert.AreEqual(40, result.PaidYen);
            Assert.AreEqual(40, wallet.Yen);
            Assert.AreEqual(40, ledger.SoldTodayYen);
            Assert.AreEqual(1, inventory.Count, "食品は買い取りの対象外。");
            Assert.IsFalse(result.CapReached);
        }

        [Test]
        public void TheDailyCap_StopsBuyingButLetsCheaperGoodsThrough()
        {
            var wallet = new Wallet(0);
            var inventory = new Inventory(ShopItems.Catalog(), capacity: 20);
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella)); // 120
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella)); // 120
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella)); // 120（3本で360 > 上限300）
            inventory.TryAdd(new ItemInstance(ShopItems.Can));      // 20
            var ledger = new SalvageLedger();

            var result = SalvageBuyback.SellSalvage(wallet, inventory, ledger, dailyCapYen: 300);

            Assert.IsTrue(result.CapReached, "上限に触れて売り切れなかった品が残る（今日はもう勘弁してくれ）。");
            Assert.LessOrEqual(ledger.SoldTodayYen, 300);
            Assert.AreEqual(300 - ledger.RemainingToday(300), ledger.SoldTodayYen);
            // 傘2本(240)＋缶(20)=260 は上限内、3本目の傘(120)は入らず残る。
            Assert.AreEqual(260, result.PaidYen);
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void OnceTheCapIsReached_ANewDayReopensIt()
        {
            var wallet = new Wallet(0);
            var inventory = new Inventory(ShopItems.Catalog(), capacity: 20);
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella));
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella));
            inventory.TryAdd(new ItemInstance(ShopItems.Umbrella));
            var ledger = new SalvageLedger();

            var first = SalvageBuyback.SellSalvage(wallet, inventory, ledger, dailyCapYen: 300);
            Assert.AreEqual(2, first.SoldCount, "2本(240)で打ち止め。3本目(120)は残り枠60に入らず残る。");
            Assert.IsTrue(first.CapReached);
            Assert.AreEqual(60, ledger.RemainingToday(300), "残り枠(60)より高い品は今日は売れない。");
            Assert.AreEqual(1, inventory.Count);

            ledger.BeginNextDay();
            Assert.AreEqual(300, ledger.RemainingToday(300), "就寝＝日境界で店主の財布が入れ替わる。");

            var second = SalvageBuyback.SellSalvage(wallet, inventory, ledger, dailyCapYen: 300);
            Assert.AreEqual(1, second.SoldCount, "残った1本を翌日に売れる。");
            Assert.AreEqual(0, inventory.Count);
        }
    }

    public sealed class JobRewardTests
    {
        [Test]
        public void FullPerformance_AtHealthySanity_PaysTheBaseWage()
        {
            Assert.AreEqual(800, JobReward.Payout(1f, sanity: 80f));
            Assert.AreEqual(400, JobReward.Payout(0.5f, sanity: 80f));
            Assert.AreEqual(0, JobReward.Payout(0f, sanity: 80f));
        }

        [Test]
        public void BelowBreakdown_TheWageCollapsesButIsNeverZeroedByDesign()
        {
            // 出来が満点でも、SAN が崩壊帯にあれば報酬は暴落する（第三節）。禁止ではない。
            var healthy = JobReward.Payout(1f, sanity: 25f);
            var broken = JobReward.Payout(1f, sanity: 15f);

            Assert.AreEqual(800, healthy);
            Assert.AreEqual(240, broken, "800 × 0.3。割に合わないが、まだできる。");
            Assert.Less(broken, healthy);
        }

        [Test]
        public void Performance_IsClampedToTheUnitRange()
        {
            Assert.AreEqual(800, JobReward.Payout(2f, sanity: 80f));
            Assert.AreEqual(0, JobReward.Payout(-1f, sanity: 80f));
        }
    }

    public sealed class SanityShopThresholdsTests
    {
        [Test]
        public void Prices_BecomeUnreadableOnlyOnBreakdown()
        {
            Assert.IsTrue(SanityScale.CanReadPrices(20f));
            Assert.IsFalse(SanityScale.CanReadPrices(19.9f), "崩壊すると値札がぼやける（第三節）。");
        }

        [Test]
        public void JobEfficiency_MatchesTheBreakdownThreshold()
        {
            Assert.AreEqual(1f, SanityScale.JobEfficiency(20f), 1e-5f);
            Assert.AreEqual(SanityScale.BrokenJobEfficiency, SanityScale.JobEfficiency(19.9f), 1e-5f);
        }
    }
}
