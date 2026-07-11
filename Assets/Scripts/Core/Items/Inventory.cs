using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 容量制インベントリ。Phase 1〜4 は「マス数のみを持つ容量制」で実装し、
    /// グリッド式（ドラッグ＆ドロップ・回転・配置可否判定）は Phase 5 に送る（第十一節）。
    ///
    /// 設計意図は容量制でも失われない。嵩張るアイテムは占有マス数が大きく、
    /// 「値の張る廃品」と「命をつなぐ食料」は同じ空間を奪い合う。
    /// </summary>
    public sealed class Inventory
    {
        /// <summary>初期装備の `ボロい肩掛けカバン`。ペットボトル3本で埋まる。</summary>
        public const int DefaultCapacity = 6;

        readonly IItemCatalog _catalog;
        readonly List<ItemInstance> _items = new List<ItemInstance>();

        int _capacity;

        public Inventory(IItemCatalog catalog, int capacity = DefaultCapacity)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public int Capacity => _capacity;

        public int UsedSlots
        {
            get
            {
                var used = 0;
                foreach (var item in _items)
                {
                    used += SlotsOf(item);
                }

                return used;
            }
        }

        public int FreeSlots => _capacity - UsedSlots;

        public int Count => _items.Count;

        public IReadOnlyList<ItemInstance> Items => _items;

        public event Action Changed;

        public ItemInstance this[int index] => _items[index];

        /// <summary>定義が見つからないアイテムは 0 マスとして扱わず、追加を拒む。</summary>
        public int SlotsOf(ItemInstance item) =>
            _catalog.TryGet(item.ItemId, out var definition) ? definition.Slots : 0;

        public bool TryGetDefinition(int index, out ItemDefinition definition)
        {
            if (index < 0 || index >= _items.Count)
            {
                definition = null;
                return false;
            }

            return _catalog.TryGet(_items[index].ItemId, out definition);
        }

        public bool CanAdd(ItemInstance item) =>
            _catalog.TryGet(item.ItemId, out var definition) && definition.Slots <= FreeSlots;

        public bool TryAdd(ItemInstance item)
        {
            if (!CanAdd(item))
            {
                return false;
            }

            _items.Add(item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 容量を増やす（バックパックの購入）。就寝時オートセーブの <see cref="InventoryState.Capacity"/> が
        /// そのまま永続するため、拡張は追加のセーブ項目を必要としない。減らす方向には使わない。
        /// </summary>
        public void Expand(int extraSlots)
        {
            if (extraSlots <= 0)
            {
                return;
            }

            _capacity += extraSlots;
            Changed?.Invoke();
        }

        public bool TryRemoveAt(int index, out ItemInstance removed)
        {
            if (index < 0 || index >= _items.Count)
            {
                removed = default;
                return false;
            }

            removed = _items[index];
            _items.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public InventoryState CaptureState() => new InventoryState
        {
            Capacity = _capacity,
            Items = new List<ItemInstance>(_items),
        };

        /// <summary>
        /// セーブデータから復元する。外部入力として扱い、定義の無いアイテムと
        /// 容量に収まらないアイテムは黙って捨てず、捨てた個数を返す。
        /// </summary>
        /// <returns>復元できずに捨てたアイテムの個数。</returns>
        public int Restore(InventoryState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _capacity = state.Capacity < 1 ? 1 : state.Capacity;
            _items.Clear();

            var dropped = 0;
            if (state.Items != null)
            {
                foreach (var item in state.Items)
                {
                    if (CanAdd(item))
                    {
                        _items.Add(item);
                    }
                    else
                    {
                        dropped++;
                    }
                }
            }

            Changed?.Invoke();
            return dropped;
        }
    }
}
