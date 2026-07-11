using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 拠点に置かれた保管庫1つ（第十二節）。カバンから財産を預け、また取り出す器である。
    ///
    /// 中身の容量制の器としての振る舞いは <see cref="Inventory"/> と同じであり、そのロジックを内包して再利用する。
    /// ただし保管庫はカバンとは別の概念である（第三節）。カバンは持ち歩くもの、保管庫は拠点に置いて離れるもので、
    /// 後続（場所代・保管庫イベント・安全性）で独立に変わっていく。だから型として分けて持つ。
    ///
    /// 出し入れの制限は種別ではなく空きマスだけである（第十一節）。預けるときは保管庫の空き、
    /// 取り出すときはカバンの空きが効く。背負い物（段ボール）は器に詰めず担ぐ物なので、内包する
    /// <see cref="Inventory.CanAdd"/> がそのまま預け入れを拒む。
    /// </summary>
    public sealed class Stash
    {
        readonly Inventory _contents;

        public Stash(IItemCatalog catalog, StashKind kind, string spotId, int capacity)
        {
            Kind = kind;
            SpotId = spotId ?? string.Empty;
            _contents = new Inventory(catalog, capacity);
        }

        public StashKind Kind { get; }

        /// <summary>この保管庫が置かれた拠点の識別子。</summary>
        public string SpotId { get; }

        public int Capacity => _contents.Capacity;
        public int UsedSlots => _contents.UsedSlots;
        public int FreeSlots => _contents.FreeSlots;
        public int Count => _contents.Count;
        public IReadOnlyList<ItemInstance> Items => _contents.Items;

        public event Action Changed
        {
            add => _contents.Changed += value;
            remove => _contents.Changed -= value;
        }

        public ItemInstance this[int index] => _contents[index];

        public bool TryGetDefinition(int index, out ItemDefinition definition) =>
            _contents.TryGetDefinition(index, out definition);

        /// <summary>預けられるか。保管庫の空きマスだけが効く（背負い物は内包側が拒む）。</summary>
        public bool CanDeposit(ItemInstance item) => _contents.CanAdd(item);

        public bool TryDeposit(ItemInstance item) => _contents.TryAdd(item);

        public bool TryWithdrawAt(int index, out ItemInstance item) => _contents.TryRemoveAt(index, out item);

        public StashState CaptureState() => new StashState
        {
            SpotId = SpotId,
            Kind = Kind,
            Capacity = Capacity,
            Items = new List<ItemInstance>(_contents.Items),
        };

        /// <summary>
        /// セーブデータから中身を復元する。外部入力として扱い、定義の無いアイテムや容量に収まらないアイテムは
        /// 内包する <see cref="Inventory.Restore"/> が黙って捨て、捨てた個数を返す。
        /// </summary>
        public int Restore(StashState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return _contents.Restore(new InventoryState { Capacity = state.Capacity, Items = state.Items });
        }
    }
}
