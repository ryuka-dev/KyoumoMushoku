using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 街の西の外れ、子ども公園の向かいの診療所（2D 版の病院・GameDesign 第三節）。
    ///
    /// 1. 倒れたら運ばれる先。目を覚ますのは戸口の前（<see cref="WakePosition"/>）で、すぐ向かいに公園の水道がある。
    ///    運ぶ流れ（医療費・SAN の代償・没収）は <see cref="ProtoSession"/> が持つ。
    /// 2. 生きているときに金を払って手当てを受ける所でもある（HP が全回復）。無料の寝床では HP はゆっくりしか戻らない。
    ///    値段は運ばれたときの医療費と同じ（<see cref="SliceTuningAsset.ClinicFeeYen"/>）。
    ///
    /// 最も安全で最も貧しい端に置く（2D 版と同じ）。奥で倒れるほど、歩いて戻る道が長い。
    /// 精神崩壊すると値段が読めない（`??円`）が、手当ては受けられる（店・安宿と同じ・第三節）。
    /// </summary>
    public sealed class ProtoClinic : MonoBehaviour, IProtoUsable
    {
        [SerializeField] SliceTuningAsset _slice;

        [Tooltip("運ばれて目を覚ます所。向きも使う。")]
        [SerializeField] Transform _wakePoint;

        [Tooltip("戸口の上の看板の字。字はテキストモジュールから入れる。")]
        [SerializeField] TextMeshPro _signText;

        public int FeeYen => _slice != null ? _slice.ClinicFeeYen : 0;

        public Vector3 WakePosition => _wakePoint != null ? _wakePoint.position : transform.position;
        public Quaternion WakeRotation => _wakePoint != null ? _wakePoint.rotation : transform.rotation;

        public void Configure(SliceTuningAsset slice, Transform wakePoint, TextMeshPro signText)
        {
            _slice = slice;
            _wakePoint = wakePoint;
            _signText = signText;
        }

        void OnEnable()
        {
            if (_signText != null)
            {
                _signText.text = ProtoText.ClinicSign;
            }
        }

        public string Prompt(ProtoHands hands)
        {
            var sanity = hands.Vitals != null && hands.Vitals.Vitals != null ? hands.Vitals.Vitals.Sanity : SanityScale.Max;
            var price = SanityScale.CanReadPrices(sanity) ? ProtoText.Price(FeeYen) : ShopText.OfferPriceUnreadable;
            return ProtoText.ClinicPrompt(price);
        }

        public string Use(ProtoHands hands)
        {
            var vitals = hands.Vitals != null ? hands.Vitals.Vitals : null;
            if (vitals == null || hands.Wallet == null)
            {
                return null;
            }

            if (vitals.Hp >= vitals.Tuning.MaxHp - 0.5f)
            {
                return ProtoText.ClinicNotNeeded;
            }

            if (!hands.Wallet.TrySpend(FeeYen))
            {
                return ProtoText.ClinicCannotAfford;
            }

            vitals.Apply(new VitalsDelta { Hp = vitals.Tuning.MaxHp });
            return ProtoText.ClinicTreated;
        }
    }
}
