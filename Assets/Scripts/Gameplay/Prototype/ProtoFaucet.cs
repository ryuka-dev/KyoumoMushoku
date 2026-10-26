using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Interaction;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 公園の水道。ただで喉を潤せる。回復量は本編と同じ <c>Assets/Config/WaterTuning.asset</c> の水道の値。
    /// 持ち運べないのが水道の代償である（水のボトルは店で買う）。
    /// </summary>
    public sealed class ProtoFaucet : MonoBehaviour, IProtoUsable
    {
        [SerializeField] WaterTuningAsset _tuning;
        [SerializeField] ProtoVitals _vitals;

        public void Configure(WaterTuningAsset tuning, ProtoVitals vitals)
        {
            _tuning = tuning;
            _vitals = vitals;
        }

        public string Prompt(ProtoHands hands) => ProtoText.FaucetPrompt;

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

            vitals.Apply(new VitalsDelta { Thirst = _tuning.TapThirstRestored });
            return ProtoText.Drank;
        }
    }
}
