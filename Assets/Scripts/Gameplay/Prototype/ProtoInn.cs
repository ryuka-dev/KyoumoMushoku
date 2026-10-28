using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.UI;
using KyoumoMushoku.Gameplay.World;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 大通りの雑居ビルの安宿（`Docs/切片の品目.md` 第六節・2D 版の安宿）。夕方から泊まれる。一泊の値段と回復は
    /// <c>SleepTuning_Slice</c> の安宿の値（1,500 円・すべて満たす）。泊まると一晩が過ぎ、日付が変わる（<see cref="ProtoSession.Sleep"/>）。
    /// 大当たりを引いた夜の使い道になる＝金が SAN に戻る道。
    ///
    /// 精神崩壊すると値段が読めない（`??円`）が、泊まることはできる（店と同じ・第三節）。
    /// </summary>
    public sealed class ProtoInn : MonoBehaviour, IProtoUsable
    {
        [SerializeField] SleepTuningAsset _tuning;
        [SerializeField] ProtoSession _session;
        [SerializeField] ProtoDayNight _dayNight;

        [Tooltip("戸口の上の看板の字。字はテキストモジュールから入れる。")]
        [SerializeField] TextMeshPro _signText;

        public void Configure(SleepTuningAsset tuning, ProtoSession session, ProtoDayNight dayNight, TextMeshPro signText)
        {
            _tuning = tuning;
            _session = session;
            _dayNight = dayNight;
            _signText = signText;
        }

        int Cost => _tuning != null ? _tuning.Inn.costYen : 0;

        bool TooEarly => _dayNight != null && _dayNight.Clock != null && _dayNight.Clock.Phase == DayPhase.Day;

        void OnEnable()
        {
            if (_signText != null)
            {
                _signText.text = ProtoText.InnSign(Cost);
            }
        }

        public string Prompt(ProtoHands hands)
        {
            var sanity = hands.Vitals != null && hands.Vitals.Vitals != null ? hands.Vitals.Vitals.Sanity : SanityScale.Max;
            var price = SanityScale.CanReadPrices(sanity) ? ProtoText.Price(Cost) : ShopText.OfferPriceUnreadable;
            return TooEarly ? ProtoText.InnTooEarly(price) : ProtoText.InnPrompt(price);
        }

        public string Use(ProtoHands hands)
        {
            if (_tuning == null || _session == null || hands.Wallet == null)
            {
                return null;
            }

            if (TooEarly)
            {
                return ProtoText.InnTooEarly(ProtoText.Price(Cost));
            }

            if (!hands.Wallet.TrySpend(Cost))
            {
                return ProtoText.InnCannotAfford;
            }

            return _session.Sleep(_tuning.Inn, atInn: true);
        }
    }
}
