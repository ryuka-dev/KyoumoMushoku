using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 寝床（公園のベンチ）。夕方から寝られる。寝ると一晩が過ぎ、日付が変わる。回復量は本編と同じ
    /// <c>Assets/Config/SleepTuning.asset</c> のベンチの値。寝たあとの流れは <see cref="ProtoSession"/> が持つ。
    /// </summary>
    public sealed class ProtoSleepSpot : MonoBehaviour, IProtoUsable
    {
        [SerializeField] SleepTuningAsset _tuning;
        [SerializeField] ProtoSession _session;
        [SerializeField] ProtoDayNight _dayNight;

        public SleepTuningAsset.Recovery Recovery =>
            _tuning != null ? _tuning.Bench : new SleepTuningAsset.Recovery { hp = 5f, sanity = 10f };

        public void Configure(SleepTuningAsset tuning, ProtoSession session, ProtoDayNight dayNight)
        {
            _tuning = tuning;
            _session = session;
            _dayNight = dayNight;
        }

        bool TooEarly => _dayNight != null && _dayNight.Clock != null && _dayNight.Clock.Phase == DayPhase.Day;

        public string Prompt(ProtoHands hands) => TooEarly ? ProtoText.BenchTooEarly : ProtoText.BenchPrompt;

        public string Use(ProtoHands hands)
        {
            if (TooEarly)
            {
                return ProtoText.BenchTooEarly;
            }

            return _session != null ? _session.Sleep(this) : null;
        }
    }
}
