using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// アーケードのリサイクルショップ（`Docs/切片の品目.md` 第六節）。値打ち物（空き箱・ゲームソフト・腕時計・ゲーム機・文庫本）を、
    /// 一日の上限なしで買う。コンビニの店主の買い取り（一日 300 円まで）の外にある出口で、大当たりを引いた日に局面が変わる。
    ///
    /// 昼だけ開き、夕方にシャッターが下りる。夜に引いた値打ち物は、翌日の昼まで鞄の枠を食う。
    /// 値段は <see cref="SliceTuningAsset.RecyclePriceOf"/>（品のデータの売値は 0 にしてあり、店主は買わない）。
    /// 精神崩壊すると値段が読めない（`??円`）が、売ることはできる（店と同じ・第三節）。
    ///
    /// 値打ち物を初めて掴んだとき、主人公がこの店を思い出す（予告は世界の中で・第十四節）。
    /// </summary>
    public sealed class ProtoRecycleShop : MonoBehaviour, IProtoUsable, IProtoClosable
    {
        [SerializeField] SliceTuningAsset _slice;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoHands _hands;

        [Tooltip("夜に下りるシャッター。上端を原点に、下へ伸びる網。開いている間は縦に縮めて巻き上げる。")]
        [SerializeField] Transform _shutter;

        [Tooltip("店先に下がる看板の字。字はテキストモジュールから入れる。")]
        [SerializeField] TextMeshPro _signText;

        [Tooltip("シャッターが下りきる（巻き上がる）までの秒数。")]
        [SerializeField, Min(0.1f)] float _shutterSeconds = 2.5f;

        float _shutterT;
        bool _hinted;

        public void Configure(SliceTuningAsset slice, ProtoDayNight dayNight, ProtoHands hands, Transform shutter)
        {
            _slice = slice;
            _dayNight = dayNight;
            _hands = hands;
            _shutter = shutter;
        }

        /// <summary>昼の間だけ開いている。夕方になったら閉まる。</summary>
        public bool IsOpenNow => _dayNight == null || _dayNight.Clock == null || _dayNight.Clock.Phase == DayPhase.Day;

        public string Prompt(ProtoHands hands)
        {
            var count = CountValuables(hands.Bag, out var yen);
            if (count == 0)
            {
                return ProtoText.RecyclePrompt;
            }

            var sanity = hands.Vitals != null && hands.Vitals.Vitals != null ? hands.Vitals.Vitals.Sanity : SanityScale.Max;
            var price = SanityScale.CanReadPrices(sanity) ? ProtoText.Price(yen) : ShopText.OfferPriceUnreadable;
            return ProtoText.RecycleSellPrompt(count, price);
        }

        public string Use(ProtoHands hands)
        {
            if (!IsOpenNow || hands.Bag == null || hands.Wallet == null)
            {
                return null;
            }

            var bag = hands.Bag;
            var sold = 0;
            var paid = 0;
            for (var i = bag.Count - 1; i >= 0; i--)
            {
                var price = PriceOf(bag[i].ItemId);
                if (price <= 0 || !bag.TryRemoveAt(i, out _))
                {
                    continue;
                }

                hands.Wallet.Add(price);
                sold++;
                paid += price;
            }

            return sold > 0 ? ProtoText.RecycleSold(sold, paid) : ProtoText.RecycleNothing;
        }

        void OnEnable()
        {
            if (_hands != null)
            {
                _hands.Grabbed += OnGrabbed;
            }

            if (_signText != null)
            {
                _signText.text = ProtoText.RecycleSign;
            }

            _shutterT = IsOpenNow ? 0f : 1f;
            ApplyShutter();
        }

        void OnDisable()
        {
            if (_hands != null)
            {
                _hands.Grabbed -= OnGrabbed;
            }
        }

        void Update()
        {
            var target = IsOpenNow ? 0f : 1f;
            if (!Mathf.Approximately(_shutterT, target))
            {
                _shutterT = Mathf.MoveTowards(_shutterT, target, Time.deltaTime / _shutterSeconds);
                ApplyShutter();
            }
        }

        void ApplyShutter()
        {
            if (_shutter == null)
            {
                return;
            }

            // 巻き上げた状態でも、シャッターボックスの下に一筋だけ残す。
            var s = _shutter.localScale;
            _shutter.localScale = new Vector3(s.x, Mathf.Lerp(0.03f, 1f, _shutterT), s.z);
        }

        void OnGrabbed(ItemInstance item)
        {
            if (_hinted || PriceOf(item.ItemId) <= 0)
            {
                return;
            }

            _hinted = true;
            _hands.Say(ProtoText.RecycleHint);
        }

        int PriceOf(ItemId item) => _slice != null ? _slice.RecyclePriceOf(item) : 0;

        int CountValuables(Inventory bag, out int yen)
        {
            yen = 0;
            var count = 0;
            if (bag == null)
            {
                return 0;
            }

            for (var i = 0; i < bag.Count; i++)
            {
                var price = PriceOf(bag[i].ItemId);
                if (price > 0)
                {
                    count++;
                    yen += price;
                }
            }

            return count;
        }
    }
}
