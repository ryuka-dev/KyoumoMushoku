using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Interaction;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// ただで飲める水。公園の水道と、公衆トイレの手洗い（<see cref="_toilet"/>）を兼ねる。
    /// 回復量は本編と同じ <c>Assets/Config/WaterTuning.asset</c> の値。持ち運べないのが代償である（水のボトルは店で買う）。
    ///
    /// 公衆トイレの水は SAN を削る＝尊厳と引き換えの生存（GameDesign 第十三節）。公衆トイレは商店街の入口にあり、
    /// 奥で喉が渇いたとき「ここで飲んで気が滅入るか、公園まで戻るか」を問う。削れることは案内と飲んだ後の一言で伝える
    /// （HUD は気分の段しか見せないので、数値の代わりに世界の言葉で言う・第十四節）。
    /// </summary>
    public sealed class ProtoFaucet : MonoBehaviour, IProtoUsable
    {
        [SerializeField] WaterTuningAsset _tuning;
        [SerializeField] ProtoVitals _vitals;

        [Tooltip("公衆トイレの手洗い。飲むと SAN が減る（WaterTuning のトイレの値）。")]
        [SerializeField] bool _toilet;

        public bool IsToilet => _toilet;

        public void Configure(WaterTuningAsset tuning, ProtoVitals vitals, bool toilet = false)
        {
            _tuning = tuning;
            _vitals = vitals;
            _toilet = toilet;
        }

        public string Prompt(ProtoHands hands) => _toilet ? ProtoText.ToiletPrompt : ProtoText.FaucetPrompt;

        public string Use(ProtoHands hands)
        {
            if (_vitals == null || _vitals.Vitals == null || _tuning == null)
            {
                return null;
            }

            var vitals = _vitals.Vitals;
            if (vitals.Thirst >= vitals.Tuning.MaxThirst - 0.5f)
            {
                return ProtoText.NotThirsty;
            }

            if (_toilet)
            {
                vitals.Apply(new VitalsDelta { Thirst = _tuning.ToiletThirstRestored, Sanity = _tuning.ToiletSanityCost });
                return ProtoText.DrankToiletWater;
            }

            vitals.Apply(new VitalsDelta { Thirst = _tuning.TapThirstRestored });
            return ProtoText.Drank;
        }
    }
}
