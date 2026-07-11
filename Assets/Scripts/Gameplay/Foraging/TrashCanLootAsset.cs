using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Foraging
{
    /// <summary>
    /// ゴミ箱の産出テーブルの保存先。種類（A/B/C）×時間帯（昼／夜）ごとに、重み付きの候補と
    /// 空振りの重み、そして食品状態の確率を持つ。ScriptableObject という Unity 固有の都合を
    /// ゲームプレイ側（<see cref="Core.Foraging.LootTable"/>）に漏らさないための境界。
    ///
    /// 重み・確率はすべて叩き台であり、プレイテストで調整する（第十三節）。
    /// コツの触発条件はゴミ箱の産出頻度に紐づくため、コツ導入時（Phase 5）に併せて見直す。
    /// </summary>
    [CreateAssetMenu(fileName = "TrashCanLoot", menuName = "KyoumoMushoku/Trash Can Loot")]
    public sealed class TrashCanLootAsset : ScriptableObject
    {
        [Serializable]
        public struct WeightedItem
        {
            public string id;
            [Min(0f)] public float weight;
        }

        [Serializable]
        public struct Table
        {
            [Tooltip("空振り（何も出ない）の重み。第十節：期待と空振りが手応えを作る。")]
            [Min(0f)] public float emptyWeight;

            public List<WeightedItem> items;

            [Header("食品状態の抽選（食品として引かれたものにだけ適用）")]
            [Min(0f)] public float freshWeight;
            [Min(0f)] public float staleWeight;
            [Min(0f)] public float rottenWeight;
        }

        [Serializable]
        public struct KindTables
        {
            public TrashCanKind kind;
            public Table day;
            public Table night;
        }

        [SerializeField] List<KindTables> _kinds = new List<KindTables>();

        Dictionary<(TrashCanKind kind, bool night), LootTable> _cache;

        /// <summary>指定の種類・時間帯のテーブルを返す。無ければ空振りだけのテーブル。</summary>
        public LootTable TableFor(TrashCanKind kind, bool night)
        {
            EnsureBuilt();
            return _cache.TryGetValue((kind, night), out var table)
                ? table
                : new LootTable(Array.Empty<LootTable.Entry>(), emptyWeight: 1f, default);
        }

        void OnDisable() => _cache = null;

        void OnValidate() => _cache = null;

        void EnsureBuilt()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new Dictionary<(TrashCanKind, bool), LootTable>();
            foreach (var kindTables in _kinds)
            {
                _cache[(kindTables.kind, false)] = Build(kindTables.day);
                _cache[(kindTables.kind, true)] = Build(kindTables.night);
            }
        }

        static LootTable Build(Table table)
        {
            var entries = new List<LootTable.Entry>();
            if (table.items != null)
            {
                foreach (var item in table.items)
                {
                    var id = new ItemId(item.id);
                    if (!id.IsEmpty)
                    {
                        entries.Add(new LootTable.Entry(id, item.weight));
                    }
                }
            }

            var odds = new FoodStateOdds(table.freshWeight, table.staleWeight, table.rottenWeight);
            return new LootTable(entries, table.emptyWeight, odds);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 第十三節のゴミ箱3種の傾向を既定値として書き込む。
        /// A（公園）：缶・瓶が中心、食品は少なく状態は平凡。
        /// B（コンビニ前）：夜に弁当・パン。状態が良い確率が高い。
        /// C（路地裏）：廃品が中心。食品は状態が悪い。
        /// 段ボール（背負いスロット・Phase 5）と店専売品（おにぎり・ペットボトルの水）は入れない。
        /// </summary>
        public void PopulateDefaults()
        {
            _kinds = new List<KindTables>
            {
                new KindTables
                {
                    kind = TrashCanKind.Park,
                    day = new Table
                    {
                        emptyWeight = 20f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "can_aluminum", weight = 35f },
                            new WeightedItem { id = "bottle_empty", weight = 20f },
                            new WeightedItem { id = "half_bottle", weight = 12f },
                            new WeightedItem { id = "bread", weight = 8f },
                            new WeightedItem { id = "cigarette", weight = 5f },
                        },
                        freshWeight = 25f, staleWeight = 45f, rottenWeight = 30f,
                    },
                    night = new Table
                    {
                        emptyWeight = 25f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "can_aluminum", weight = 33f },
                            new WeightedItem { id = "bottle_empty", weight = 20f },
                            new WeightedItem { id = "half_bottle", weight = 12f },
                            new WeightedItem { id = "bread", weight = 5f },
                            new WeightedItem { id = "cigarette", weight = 5f },
                        },
                        freshWeight = 20f, staleWeight = 45f, rottenWeight = 35f,
                    },
                },
                new KindTables
                {
                    kind = TrashCanKind.ConvenienceStore,
                    day = new Table
                    {
                        emptyWeight = 45f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "bread", weight = 20f },
                            new WeightedItem { id = "can_aluminum", weight = 25f },
                            new WeightedItem { id = "bottle_empty", weight = 10f },
                        },
                        freshWeight = 45f, staleWeight = 40f, rottenWeight = 15f,
                    },
                    night = new Table
                    {
                        // 夜のコンビニが最も実り多い。第十三節・第二節の夜間テーブル。
                        emptyWeight = 8f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "bento", weight = 35f },
                            new WeightedItem { id = "bread", weight = 25f },
                            new WeightedItem { id = "can_coffee", weight = 12f },
                            new WeightedItem { id = "can_aluminum", weight = 12f },
                            new WeightedItem { id = "bottle_empty", weight = 8f },
                        },
                        freshWeight = 65f, staleWeight = 28f, rottenWeight = 7f,
                    },
                },
                new KindTables
                {
                    kind = TrashCanKind.BackAlley,
                    day = new Table
                    {
                        emptyWeight = 18f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "umbrella_broken", weight = 15f },
                            new WeightedItem { id = "bottle_empty", weight = 25f },
                            new WeightedItem { id = "can_aluminum", weight = 25f },
                            new WeightedItem { id = "cardboard", weight = 12f },
                            new WeightedItem { id = "bread", weight = 12f },
                            new WeightedItem { id = "cigarette", weight = 5f },
                        },
                        freshWeight = 8f, staleWeight = 32f, rottenWeight = 60f,
                    },
                    night = new Table
                    {
                        emptyWeight = 18f,
                        items = new List<WeightedItem>
                        {
                            new WeightedItem { id = "umbrella_broken", weight = 15f },
                            new WeightedItem { id = "bottle_empty", weight = 22f },
                            new WeightedItem { id = "can_aluminum", weight = 25f },
                            new WeightedItem { id = "cardboard", weight = 12f },
                            new WeightedItem { id = "bread", weight = 12f },
                            new WeightedItem { id = "cigarette", weight = 8f },
                        },
                        freshWeight = 8f, staleWeight = 32f, rottenWeight = 60f,
                    },
                },
            };

            _cache = null;
        }
#endif
    }
}
