using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 店の中で手を伸ばす先。回収箱（廃品をまとめて売る）か、品の棚（一つ買う）。取引は <see cref="ProtoStore"/> が行う。
    /// </summary>
    public sealed class ProtoCounter : MonoBehaviour, IProtoUsable
    {
        public enum Kind
        {
            Buyback,
            Buy,
        }

        [SerializeField] ProtoStore _store;
        [SerializeField] Kind _kind;
        [Tooltip("棚に並ぶ品の ID（Buy のとき）。ItemDatabase の id。")]
        [SerializeField] string _itemId;

        public void Configure(ProtoStore store, Kind kind, string itemId = null)
        {
            _store = store;
            _kind = kind;
            _itemId = itemId;
        }

        public string Prompt(ProtoHands hands)
        {
            if (_store == null)
            {
                return null;
            }

            if (_kind == Kind.Buyback)
            {
                return _store.SellPrompt(hands.Bag);
            }

            var sanity = hands.Vitals != null && hands.Vitals.Vitals != null ? hands.Vitals.Vitals.Sanity : 100f;
            return _store.BuyPrompt(_itemId, hands.Bag, sanity);
        }

        public string Use(ProtoHands hands)
        {
            if (_store == null)
            {
                return null;
            }

            return _kind == Kind.Buyback
                ? _store.Sell(hands.Bag, hands.Wallet)
                : _store.Buy(_itemId, hands.Bag, hands.Wallet);
        }
    }
}
