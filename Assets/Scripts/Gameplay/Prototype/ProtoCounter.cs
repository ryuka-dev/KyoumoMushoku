using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 店の中で手を伸ばす先（回収箱・バックパックの棚）。左クリックで <see cref="ProtoStore"/> の取引を一つ行う。
    /// コライダーと同じ物体か、その親に付ける。
    /// </summary>
    public sealed class ProtoCounter : MonoBehaviour
    {
        public enum Kind
        {
            Buyback,
            Backpack,
        }

        [SerializeField] ProtoStore _store;
        [SerializeField] Kind _kind;

        public void Configure(ProtoStore store, Kind kind)
        {
            _store = store;
            _kind = kind;
        }

        public string Prompt(ProtoHands hands)
        {
            if (_store == null)
            {
                return null;
            }

            return _kind == Kind.Buyback ? _store.SellPrompt(hands.Bag) : _store.BackpackPrompt(hands.Bag);
        }

        public string Use(ProtoHands hands)
        {
            if (_store == null)
            {
                return null;
            }

            return _kind == Kind.Buyback
                ? _store.Sell(hands.Bag, hands.Wallet)
                : _store.BuyBackpack(hands.Bag, hands.Wallet);
        }
    }
}
