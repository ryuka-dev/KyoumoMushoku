using System;
using KyoumoMushoku.Core.Items;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Items
{
    /// <summary>
    /// 持ち物の所有者。<see cref="Inventory"/> をシーンに接続し、アイテムカタログを供給する。
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] int _startingCapacity = Inventory.DefaultCapacity;

        public Inventory Inventory { get; private set; }
        public IItemCatalog Catalog => _catalog;

        public void Configure(ItemDatabaseAsset catalog) => _catalog = catalog;

        void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"{nameof(PlayerInventory)}: {nameof(ItemDatabaseAsset)} が割り当てられていない。", this);
                enabled = false;
                return;
            }

            Inventory ??= new Inventory(_catalog, _startingCapacity);
        }

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void RestoreState(InventoryState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Inventory = new Inventory(_catalog);
            Inventory.Restore(state);
        }
    }
}
