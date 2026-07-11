using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    sealed class FakeCatalog : IItemCatalog
    {
        readonly Dictionary<ItemId, ItemDefinition> _definitions = new Dictionary<ItemId, ItemDefinition>();

        public FakeCatalog Add(ItemDefinition definition)
        {
            _definitions[definition.Id] = definition;
            return this;
        }

        public bool TryGet(ItemId id, out ItemDefinition definition) => _definitions.TryGetValue(id, out definition);
    }

    static class TestItems
    {
        public static readonly ItemId WaterBottle = new ItemId("water_bottle");
        public static readonly ItemId Onigiri = new ItemId("onigiri");
        public static readonly ItemId Bento = new ItemId("bento");
        public static readonly ItemId Cardboard = new ItemId("cardboard");
        public static readonly ItemId Unknown = new ItemId("does_not_exist");

        public static FakeCatalog Catalog() => new FakeCatalog()
            .Add(new ItemDefinition(WaterBottle, "ペットボトルの水", ItemCategory.Water, slots: 2,
                effect: new VitalsDelta { Thirst = 40f }))
            .Add(new ItemDefinition(Onigiri, "おにぎり", ItemCategory.Food, slots: 1,
                effect: new VitalsDelta { Hunger = 25f }))
            .Add(new ItemDefinition(Bento, "コンビニ弁当", ItemCategory.Food, slots: 2,
                effect: new VitalsDelta { Hunger = 40f },
                rottenPenalty: new VitalsDelta { Hp = -12f, Sanity = -8f }))
            .Add(new ItemDefinition(Cardboard, "段ボール", ItemCategory.Salvage, slots: 6,
                sellPriceYen: 30, carriedOnBack: true));
    }

    public sealed class FoodKnowledgeTests
    {
        [TestCase(FoodState.Rotten, 100f, FoodStateReading.Exact)]
        [TestCase(FoodState.Rotten, 50f, FoodStateReading.Exact)]
        [TestCase(FoodState.Rotten, 49.9f, FoodStateReading.NotFresh)]
        [TestCase(FoodState.Stale, 20f, FoodStateReading.NotFresh)]
        [TestCase(FoodState.Fresh, 20f, FoodStateReading.Exact)]
        [TestCase(FoodState.Fresh, 19.9f, FoodStateReading.Unknown)]
        [TestCase(FoodState.Rotten, 0f, FoodStateReading.Unknown)]
        public void Read_MatchesTheFoodCardTable(FoodState state, float sanity, FoodStateReading expected)
        {
            Assert.AreEqual(expected, FoodKnowledge.Read(state, sanity));
        }

        [Test]
        public void Consequences_AreHiddenWheneverTheStateIsNotExact()
        {
            Assert.IsTrue(FoodKnowledge.KnowsConsequences(FoodStateReading.Exact));
            Assert.IsFalse(FoodKnowledge.KnowsConsequences(FoodStateReading.NotFresh));
            Assert.IsFalse(FoodKnowledge.KnowsConsequences(FoodStateReading.Unknown));
        }

        [Test]
        public void HungerRecovery_NeverDependsOnState()
        {
            var bento = new ItemDefinition(TestItems.Bento, "コンビニ弁当", ItemCategory.Food, slots: 2,
                effect: new VitalsDelta { Hunger = 40f },
                rottenPenalty: new VitalsDelta { Hp = -12f, Sanity = -8f });

            foreach (var state in new[] { FoodState.Fresh, FoodState.Stale, FoodState.Rotten })
            {
                Assert.AreEqual(40f, bento.EffectFor(state).Hunger, 1e-5f,
                    "弁当は弁当であり、腐っていても同じだけ腹にたまる。");
            }
        }

        [Test]
        public void OnlyRotten_CostsHpAndSanity()
        {
            TestItems.Catalog().TryGet(TestItems.Bento, out var definition);

            Assert.AreEqual(0f, definition.EffectFor(FoodState.Fresh).Hp, 1e-5f);
            Assert.AreEqual(0f, definition.EffectFor(FoodState.Stale).Hp, 1e-5f,
                "傷みは危険ではない。読めないことだけが危険である。");
            Assert.AreEqual(-12f, definition.EffectFor(FoodState.Rotten).Hp, 1e-5f);
            Assert.AreEqual(-8f, definition.EffectFor(FoodState.Rotten).Sanity, 1e-5f);
        }
    }

    public sealed class ConsumptionTests
    {
        [Test]
        public void EatingFood_RestoresHungerAndRemovesTheItem()
        {
            var inventory = new Inventory(TestItems.Catalog());
            inventory.TryAdd(new ItemInstance(TestItems.Onigiri));
            var vitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 50f });

            var result = Consumption.TryConsume(inventory, 0, vitals, out var consumed);

            Assert.AreEqual(ConsumeResult.Consumed, result);
            Assert.AreEqual(TestItems.Onigiri, consumed.Id);
            Assert.AreEqual(75f, vitals.Hunger, 1e-3f);
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void RottenFood_FillsTheSameButAlsoWounds()
        {
            var fresh = new Inventory(TestItems.Catalog());
            fresh.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Fresh));
            var freshVitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 0f, Hp = 100f, Sanity = 100f });
            Consumption.TryConsume(fresh, 0, freshVitals, out _);

            var rotten = new Inventory(TestItems.Catalog());
            rotten.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Rotten));
            var rottenVitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 0f, Hp = 100f, Sanity = 100f });
            Consumption.TryConsume(rotten, 0, rottenVitals, out _);

            Assert.AreEqual(freshVitals.Hunger, rottenVitals.Hunger, 1e-5f, "腐っていても同じだけ腹にたまる。");
            Assert.Less(rottenVitals.Hp, freshVitals.Hp, "腐敗だけが HP を削る。");
            Assert.Less(rottenVitals.Sanity, freshVitals.Sanity, "腐敗だけが SAN を削る。");
        }

        [Test]
        public void IronStomach_HalvesTheRottenPenalty_ButNeverTheHungerFilled()
        {
            var plain = new Inventory(TestItems.Catalog());
            plain.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Rotten));
            var plainVitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 0f, Hp = 100f, Sanity = 100f });
            Consumption.TryConsume(plain, 0, plainVitals, out _);

            var tough = new Inventory(TestItems.Catalog());
            tough.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Rotten));
            var toughVitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 0f, Hp = 100f, Sanity = 100f });
            var book = new Knacks.KnackBook();
            book.RecordSurvivedRotten();
            Consumption.TryConsume(tough, 0, toughVitals, out _, book);

            Assert.AreEqual(plainVitals.Hunger, toughVitals.Hunger, 1e-5f, "腹の膨れ方はコツに依存しない。");
            Assert.AreEqual(100f - (100f - plainVitals.Hp) * 0.5f, toughVitals.Hp, 1e-3f, "腐敗の HP ダメージは半減する。");
            Assert.AreEqual(100f - (100f - plainVitals.Sanity) * 0.5f, toughVitals.Sanity, 1e-3f, "腐敗の SAN ダメージも半減する。");
        }

        [Test]
        public void IronStomach_DoesNothingToFreshFood()
        {
            var tough = new Inventory(TestItems.Catalog());
            tough.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Fresh));
            var vitals = new Vitals(new VitalsTuning(), new VitalsState { Hunger = 0f, Hp = 100f, Sanity = 100f });
            var book = new Knacks.KnackBook();
            book.RecordSurvivedRotten();

            Consumption.TryConsume(tough, 0, vitals, out _, book);

            Assert.AreEqual(100f, vitals.Hp, 1e-5f, "新鮮な食品には何の代償もない。");
            Assert.AreEqual(100f, vitals.Sanity, 1e-5f);
        }

        [Test]
        public void Salvage_CannotBeEaten()
        {
            var catalog = TestItems.Catalog()
                .Add(new ItemDefinition(new ItemId("can_aluminum"), "アルミ缶", ItemCategory.Salvage, slots: 1, sellPriceYen: 20));
            var inventory = new Inventory(catalog);
            inventory.TryAdd(new ItemInstance(new ItemId("can_aluminum")));
            var vitals = new Vitals(new VitalsTuning());

            Assert.AreEqual(ConsumeResult.NotConsumable, Consumption.TryConsume(inventory, 0, vitals, out _));
            Assert.AreEqual(1, inventory.Count, "飲食できない物は消えない。");
        }

        [Test]
        public void EmptySlot_ReportsNoItem()
        {
            var inventory = new Inventory(TestItems.Catalog());
            var vitals = new Vitals(new VitalsTuning());
            Assert.AreEqual(ConsumeResult.NoItem, Consumption.TryConsume(inventory, 0, vitals, out _));
        }
    }

    public sealed class CarrySlotTests
    {
        [Test]
        public void OnlyCarriedOnBackItems_CanBeCarried()
        {
            var slot = new CarrySlot(TestItems.Catalog());

            Assert.IsFalse(slot.CanCarry(new ItemInstance(TestItems.Onigiri)), "鞄物は担がない。");
            Assert.IsTrue(slot.CanCarry(new ItemInstance(TestItems.Cardboard)));
            Assert.IsTrue(slot.TryCarry(new ItemInstance(TestItems.Cardboard)));
            Assert.IsTrue(slot.IsOccupied);
        }

        [Test]
        public void OnlyOneThingAtATime_TheSlotHoldsASingleCardboard()
        {
            var slot = new CarrySlot(TestItems.Catalog());
            slot.TryCarry(new ItemInstance(TestItems.Cardboard));

            Assert.IsFalse(slot.CanCarry(new ItemInstance(TestItems.Cardboard)), "両手はふさがっている。");
            Assert.IsFalse(slot.TryCarry(new ItemInstance(TestItems.Cardboard)));
        }

        [Test]
        public void TakeOut_EmptiesTheSlotAndReturnsTheItem()
        {
            var slot = new CarrySlot(TestItems.Catalog());
            slot.TryCarry(new ItemInstance(TestItems.Cardboard));

            Assert.IsTrue(slot.TryTakeOut(out var taken));
            Assert.AreEqual(TestItems.Cardboard, taken.ItemId);
            Assert.IsFalse(slot.IsOccupied);
            Assert.IsFalse(slot.TryTakeOut(out _), "空のスロットからは何も出ない。");
        }

        [Test]
        public void CaptureAndRestore_RoundTrips()
        {
            var slot = new CarrySlot(TestItems.Catalog());
            slot.TryCarry(new ItemInstance(TestItems.Cardboard));

            var restored = new CarrySlot(TestItems.Catalog());
            Assert.AreEqual(0, restored.Restore(slot.CaptureState()));
            Assert.IsTrue(restored.IsOccupied);
            Assert.AreEqual(TestItems.Cardboard, restored.Item.ItemId);
        }

        [Test]
        public void Restore_TreatsSaveDataAsUntrustedInput()
        {
            // 背負い物でないものが載っていたら、担がずに捨てる。
            var badState = new CarrySlotState { Occupied = true, Item = new ItemInstance(TestItems.Onigiri) };
            var slot = new CarrySlot(TestItems.Catalog());

            Assert.AreEqual(1, slot.Restore(badState), "背負い物でないものは担がず、捨てた個数を返す。");
            Assert.IsFalse(slot.IsOccupied);

            var emptyState = new CarrySlotState();
            Assert.AreEqual(0, slot.Restore(emptyState));
            Assert.IsFalse(slot.IsOccupied);
        }
    }

    public sealed class InventoryTests
    {
        [Test]
        public void DefaultCapacity_HoldsExactlyThreeBottlesOfWater()
        {
            var inventory = new Inventory(TestItems.Catalog());

            Assert.IsTrue(inventory.TryAdd(new ItemInstance(TestItems.WaterBottle)));
            Assert.IsTrue(inventory.TryAdd(new ItemInstance(TestItems.WaterBottle)));
            Assert.IsTrue(inventory.TryAdd(new ItemInstance(TestItems.WaterBottle)));

            Assert.AreEqual(0, inventory.FreeSlots);
            Assert.IsFalse(inventory.TryAdd(new ItemInstance(TestItems.Onigiri)),
                "水を多く持つほど食料を運べなくなる。");
        }

        [Test]
        public void BulkyItems_CostMoreSlots()
        {
            var inventory = new Inventory(TestItems.Catalog(), capacity: 3);
            inventory.TryAdd(new ItemInstance(TestItems.Bento));

            Assert.AreEqual(2, inventory.UsedSlots);
            Assert.AreEqual(1, inventory.FreeSlots);
            Assert.IsTrue(inventory.TryAdd(new ItemInstance(TestItems.Onigiri)));
            Assert.AreEqual(0, inventory.FreeSlots);
        }

        [Test]
        public void UnknownItems_AreRefused()
        {
            var inventory = new Inventory(TestItems.Catalog());

            Assert.IsFalse(inventory.CanAdd(new ItemInstance(TestItems.Unknown)));
            Assert.IsFalse(inventory.TryAdd(new ItemInstance(TestItems.Unknown)));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void CarriedOnBackItems_NeverEnterTheBag()
        {
            var inventory = new Inventory(TestItems.Catalog(), capacity: 12);

            Assert.IsFalse(inventory.CanAdd(new ItemInstance(TestItems.Cardboard)),
                "段ボールは鞄に入れず、背負いスロットで運ぶ。");
            Assert.IsFalse(inventory.TryAdd(new ItemInstance(TestItems.Cardboard)));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void RemoveAt_ReturnsTheInstanceWithItsState()
        {
            var inventory = new Inventory(TestItems.Catalog());
            inventory.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Rotten));

            Assert.IsTrue(inventory.TryRemoveAt(0, out var removed));
            Assert.AreEqual(TestItems.Bento, removed.ItemId);
            Assert.AreEqual(FoodState.Rotten, removed.Freshness);
            Assert.AreEqual(0, inventory.Count);

            Assert.IsFalse(inventory.TryRemoveAt(0, out _));
        }

        [Test]
        public void Restore_RoundTripsAndDropsWhatItCannotHold()
        {
            var source = new Inventory(TestItems.Catalog());
            source.TryAdd(new ItemInstance(TestItems.Bento, FoodState.Stale));
            source.TryAdd(new ItemInstance(TestItems.Onigiri));

            var restored = new Inventory(TestItems.Catalog());
            Assert.AreEqual(0, restored.Restore(source.CaptureState()));
            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual(FoodState.Stale, restored[0].Freshness);
        }

        [Test]
        public void Restore_TreatsSaveDataAsUntrustedInput()
        {
            var state = new InventoryState
            {
                Capacity = 0,
                Items = new List<ItemInstance>
                {
                    new ItemInstance(TestItems.Unknown),
                    new ItemInstance(TestItems.Bento),
                    new ItemInstance(TestItems.Onigiri),
                },
            };

            var inventory = new Inventory(TestItems.Catalog());
            var dropped = inventory.Restore(state);

            Assert.AreEqual(1, inventory.Capacity, "容量は最低でも1マスに正される。");
            Assert.AreEqual(1, inventory.Count, "定義の無いアイテムと入りきらないアイテムは捨てられる。");
            Assert.AreEqual(TestItems.Onigiri, inventory[0].ItemId);
            Assert.AreEqual(2, dropped, "捨てた個数は黙って隠さず呼び出し側に返す。");
        }
    }
}
