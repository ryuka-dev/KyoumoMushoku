using KyoumoMushoku.Core.Items;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// 地面に落ちている拾える1個体。Phase 1 では、ゴミ箱（Phase 2）と店（Phase 4）が
    /// まだ無いあいだ食料と水を供給する足場として使う。設計意図（容量制の取捨選択）は
    /// ここで既に働く。カバンが一杯なら拾えない。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] string _itemId;
        [SerializeField] FoodState _freshness = FoodState.Fresh;

        public void Configure(ItemId id, FoodState freshness)
        {
            _itemId = id.Value;
            _freshness = freshness;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        ItemInstance Instance => new ItemInstance(new ItemId(_itemId), _freshness);

        public bool CanInteract(PlayerContext player) =>
            player.Inventory != null && player.Inventory.Inventory.CanAdd(Instance);

        public string Describe(PlayerContext player)
        {
            if (player.Inventory == null)
            {
                return string.Empty;
            }

            var catalog = player.Inventory.Catalog;
            var name = catalog != null && catalog.TryGet(new ItemId(_itemId), out var definition)
                ? definition.DisplayName
                : _itemId;

            return player.Inventory.Inventory.CanAdd(Instance) ? $"{name}を拾う" : $"{name}（カバンが一杯だ）";
        }

        public void Interact(PlayerContext player)
        {
            if (player.Inventory.Inventory.TryAdd(Instance))
            {
                Destroy(gameObject);
            }
        }
    }
}
