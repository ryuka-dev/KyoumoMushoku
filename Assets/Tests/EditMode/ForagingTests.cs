using System.Collections.Generic;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 抽選に渡す一様乱数を台本どおりに返す。境界（空振り・候補の選択・食品状態）を決定的に検証するため。
    /// </summary>
    sealed class ScriptedRng : IRng
    {
        readonly Queue<double> _values;

        public ScriptedRng(params double[] values) => _values = new Queue<double>(values);

        public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : 0d;
    }

    public sealed class FoodStateOddsTests
    {
        [Test]
        public void Roll_PicksTheBandThatContainsTheRoll()
        {
            var odds = new FoodStateOdds(fresh: 10f, stale: 30f, rotten: 60f); // total 100

            Assert.AreEqual(FoodState.Fresh, new FoodStateOdds(10f, 30f, 60f).Roll(new ScriptedRng(0.05d)));  // 5 < 10
            Assert.AreEqual(FoodState.Stale, odds.Roll(new ScriptedRng(0.20d)));   // 20 < 40
            Assert.AreEqual(FoodState.Rotten, odds.Roll(new ScriptedRng(0.90d)));  // 90 >= 40
        }

        [Test]
        public void Roll_AllZeroWeights_FallsBackToStale_NotRotten()
        {
            // 危険な腐敗を既定にしない。傷みは危険ではなく、読めないことだけが危険である（第十一節）。
            Assert.AreEqual(FoodState.Stale, new FoodStateOdds(0f, 0f, 0f).Roll(new ScriptedRng(0.5d)));
        }

        [Test]
        public void Roll_NegativeWeights_AreTreatedAsZero()
        {
            var odds = new FoodStateOdds(fresh: -5f, stale: 0f, rotten: 10f);
            Assert.AreEqual(FoodState.Rotten, odds.Roll(new ScriptedRng(0.5d)));
        }
    }

    public sealed class LootTableTests
    {
        static readonly ItemId Bread = new ItemId("bread");
        static readonly ItemId Can = new ItemId("can_aluminum");

        static IItemCatalog Catalog() => new FakeCatalog()
            .Add(new ItemDefinition(Bread, "パン", ItemCategory.Food, slots: 2,
                effect: new VitalsDelta { Hunger = 22f },
                rottenPenalty: new VitalsDelta { Hp = -10f, Sanity = -6f }))
            .Add(new ItemDefinition(Can, "アルミ缶", ItemCategory.Salvage, slots: 1, sellPriceYen: 20));

        [Test]
        public void Draw_ReturnsEmpty_WhenRollLandsInTheEmptyBand()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(Can, 50f) },
                emptyWeight: 50f,
                default); // total 100

            var draw = table.Draw(new ScriptedRng(0.20d), Catalog()); // 20 < 50 → 空振り

            Assert.IsFalse(draw.HasItem);
        }

        [Test]
        public void Draw_SelectsTheItem_WhenRollLandsPastTheEmptyBand()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(Can, 50f) },
                emptyWeight: 50f,
                default);

            var draw = table.Draw(new ScriptedRng(0.80d), Catalog()); // 80 >= 50 → 当たり

            Assert.IsTrue(draw.HasItem);
            Assert.AreEqual(Can, draw.Item);
        }

        [Test]
        public void Draw_NonFood_IsAlwaysFresh_AndConsumesNoStateRoll()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(Can, 100f) },
                emptyWeight: 0f,
                new FoodStateOdds(0f, 0f, 100f)); // 食品なら腐敗になる重み

            // rng は1つだけ。食品以外なら状態抽選を引かないので、これで足りる。
            var draw = table.Draw(new ScriptedRng(0.50d), Catalog());

            Assert.IsTrue(draw.HasItem);
            Assert.AreEqual(Can, draw.Item);
            Assert.AreEqual(FoodState.Fresh, draw.State, "食品以外は状態を持たない。");
        }

        [Test]
        public void Draw_Food_RollsItsStateFromTheTablesOdds()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(Bread, 100f) },
                emptyWeight: 0f,
                new FoodStateOdds(fresh: 10f, stale: 10f, rotten: 80f)); // total 100

            // 1つ目で候補（パン）を選び、2つ目で状態を引く。95 >= 20 → 腐敗。
            var draw = table.Draw(new ScriptedRng(0.50d, 0.95d), Catalog());

            Assert.IsTrue(draw.HasItem);
            Assert.AreEqual(Bread, draw.Item);
            Assert.AreEqual(FoodState.Rotten, draw.State);
        }

        [Test]
        public void Draw_Food_CanAlsoRollFresh()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(Bread, 100f) },
                emptyWeight: 0f,
                new FoodStateOdds(fresh: 60f, stale: 30f, rotten: 10f));

            var draw = table.Draw(new ScriptedRng(0.50d, 0.10d), Catalog()); // 10 < 60 → 新鮮

            Assert.AreEqual(FoodState.Fresh, draw.State);
        }

        [Test]
        public void Draw_EmptyTable_YieldsEmpty()
        {
            var table = new LootTable(new LootTable.Entry[0], emptyWeight: 0f, default);
            Assert.IsFalse(table.Draw(new ScriptedRng(0.5d), Catalog()).HasItem);
        }

        [Test]
        public void Draw_ItemNotInCatalog_IsTreatedAsNonFood()
        {
            var table = new LootTable(
                new[] { new LootTable.Entry(new ItemId("mystery"), 100f) },
                emptyWeight: 0f,
                new FoodStateOdds(0f, 0f, 100f));

            var draw = table.Draw(new ScriptedRng(0.5d), Catalog());

            Assert.IsTrue(draw.HasItem);
            Assert.AreEqual(FoodState.Fresh, draw.State, "定義が無ければ食品扱いしない。");
        }
    }
}
