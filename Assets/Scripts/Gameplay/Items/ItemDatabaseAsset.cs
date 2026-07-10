using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Items
{
    /// <summary>
    /// アイテム定義の保存先。<see cref="IItemCatalog"/> の唯一の実装であり、
    /// ScriptableObject という Unity 固有の都合をゲームプレイ側に漏らさないための境界。
    ///
    /// 占有マス数・回復量・売値はすべて叩き台であり、プレイテストで調整する（第十一節）。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "KyoumoMushoku/Item Database")]
    public sealed class ItemDatabaseAsset : ScriptableObject, IItemCatalog
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public string displayName;
            public ItemCategory category;
            [Min(1)] public int slots;

            [Tooltip("状態に依存しない効果。食品なら空腹の回復量。水なら渇きの回復量。")]
            public float hp;
            public float thirst;
            public float hunger;
            public float sanity;

            [Tooltip("腐敗しているときにのみ、上の効果に加えて適用される代償（負値）。")]
            public float rottenHp;
            public float rottenSanity;

            public int sellPriceYen;
        }

        [SerializeField] List<Entry> _entries = new List<Entry>();

        Dictionary<ItemId, ItemDefinition> _byId;

        public bool TryGet(ItemId id, out ItemDefinition definition)
        {
            EnsureBuilt();
            return _byId.TryGetValue(id, out definition);
        }

        public IEnumerable<ItemDefinition> All()
        {
            EnsureBuilt();
            return _byId.Values;
        }

        void OnDisable() => _byId = null;

        void OnValidate() => _byId = null;

        void EnsureBuilt()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<ItemId, ItemDefinition>();
            foreach (var entry in _entries)
            {
                var id = new ItemId(entry.id);
                if (id.IsEmpty)
                {
                    Debug.LogWarning($"{name}: 識別子が空のアイテム定義を飛ばした。", this);
                    continue;
                }

                if (_byId.ContainsKey(id))
                {
                    Debug.LogError($"{name}: アイテム識別子 '{id}' が重複している。", this);
                    continue;
                }

                _byId[id] = new ItemDefinition(
                    id,
                    string.IsNullOrWhiteSpace(entry.displayName) ? entry.id : entry.displayName,
                    entry.category,
                    Mathf.Max(1, entry.slots),
                    new VitalsDelta { Hp = entry.hp, Thirst = entry.thirst, Hunger = entry.hunger, Sanity = entry.sanity },
                    new VitalsDelta { Hp = entry.rottenHp, Sanity = entry.rottenSanity },
                    entry.sellPriceYen);
            }
        }

#if UNITY_EDITOR
        /// <summary>初版・全15種のうち Phase 1 で意味を持つものを既定値として書き込む（第十一節）。</summary>
        public void PopulateDefaults()
        {
            _entries = new List<Entry>
            {
                new Entry { id = "water_bottle", displayName = "ペットボトルの水", category = ItemCategory.Water, slots = 2, thirst = 45f },
                new Entry { id = "half_bottle", displayName = "飲みかけのペットボトル", category = ItemCategory.Water, slots = 2, thirst = 18f, sanity = -4f },
                new Entry { id = "onigiri", displayName = "おにぎり", category = ItemCategory.Food, slots = 1, hunger = 25f },
                new Entry { id = "bento", displayName = "コンビニ弁当", category = ItemCategory.Food, slots = 2, hunger = 40f, rottenHp = -14f, rottenSanity = -8f },
                new Entry { id = "bread", displayName = "パン", category = ItemCategory.Food, slots = 2, hunger = 22f, rottenHp = -10f, rottenSanity = -6f },
                new Entry { id = "can_aluminum", displayName = "アルミ缶", category = ItemCategory.Salvage, slots = 1, sellPriceYen = 20 },
                new Entry { id = "bottle_empty", displayName = "空き瓶", category = ItemCategory.Salvage, slots = 2, sellPriceYen = 50 },
                new Entry { id = "umbrella_broken", displayName = "壊れた傘", category = ItemCategory.Salvage, slots = 3, sellPriceYen = 120 },
                new Entry { id = "can_coffee", displayName = "缶コーヒー", category = ItemCategory.Consumable, slots = 1, thirst = 10f, sanity = 12f },
                new Entry { id = "cigarette", displayName = "拾ったタバコ", category = ItemCategory.Consumable, slots = 1, hp = -3f, sanity = 10f },
            };
            _byId = null;
        }
#endif
    }
}
