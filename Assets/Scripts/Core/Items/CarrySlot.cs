using System;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 背負いスロット（第十一節）。グリッド／容量のカバンとは独立した1枠で、段ボールのように
    /// 担いで運ぶ物（<see cref="ItemDefinition.CarriedOnBack"/>）だけが1個入る。
    ///
    /// 背負っている間の代償（移動減速・走行不可・警察の注目上昇）はスロット自身は持たない。
    /// それらは既存の仕組み（<c>IMovementSpeedModifier</c>・警察システム）へ Gameplay 側で流し込む。
    /// </summary>
    public sealed class CarrySlot
    {
        readonly IItemCatalog _catalog;
        bool _occupied;
        ItemInstance _item;

        public CarrySlot(IItemCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool IsOccupied => _occupied;

        /// <summary>担いでいる物。意味を持つのは <see cref="IsOccupied"/> のときだけ。</summary>
        public ItemInstance Item => _item;

        public event Action Changed;

        /// <summary>担げるのは背負い物だけで、しかもスロットが空いているときだけ。</summary>
        public bool CanCarry(ItemInstance item) =>
            !_occupied && _catalog.TryGet(item.ItemId, out var definition) && definition.CarriedOnBack;

        public bool TryCarry(ItemInstance item)
        {
            if (!CanCarry(item))
            {
                return false;
            }

            _item = item;
            _occupied = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>担いでいる物を下ろす。設置（保管庫化）・売却・寝具のいずれもここを通る。</summary>
        public bool TryTakeOut(out ItemInstance item)
        {
            if (!_occupied)
            {
                item = default;
                return false;
            }

            item = _item;
            _item = default;
            _occupied = false;
            Changed?.Invoke();
            return true;
        }

        public CarrySlotState CaptureState() => new CarrySlotState { Occupied = _occupied, Item = _item };

        /// <summary>
        /// セーブデータから復元する。外部入力として扱い、背負い物でないもの・定義の無いものは
        /// 黙って担がず、捨てた個数（0 か 1）を返す。
        /// </summary>
        public int Restore(CarrySlotState state)
        {
            _occupied = false;
            _item = default;

            if (state is null || !state.Occupied)
            {
                Changed?.Invoke();
                return 0;
            }

            if (_catalog.TryGet(state.Item.ItemId, out var definition) && definition.CarriedOnBack)
            {
                _item = state.Item;
                _occupied = true;
                Changed?.Invoke();
                return 0;
            }

            Changed?.Invoke();
            return 1;
        }
    }
}
