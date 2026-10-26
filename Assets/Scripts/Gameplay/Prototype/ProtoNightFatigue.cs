using KyoumoMushoku.Core.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 夜更かし（作業計画 第一の二節 決定事項「切片の SAN」③）。夜は長さに上限が無いので、ある時刻を過ぎたら、
    /// 遅くなるほど SAN の減りを急にする。始まりは廃棄の時間より後に置く：廃棄を待つのは軽い代償、夜通し粘るのは重い代償。
    ///
    /// 減りが急になったことは、世界の中で伝える（第十四節）。数値ではなく独り言で。
    /// 寝れば日付が変わり、また昼から始まる（<see cref="ProtoDayNight.SecondsSinceNightfall"/> が 0 に戻る）。
    /// </summary>
    public sealed class ProtoNightFatigue : MonoBehaviour
    {
        [SerializeField] SliceTuningAsset _tuning;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoHands _player;

        int _day;
        bool _saidDrowsy;
        bool _saidExhausted;

        public void Configure(SliceTuningAsset tuning, ProtoDayNight dayNight, ProtoHands player)
        {
            _tuning = tuning;
            _dayNight = dayNight;
            _player = player;
        }

        /// <summary>いま夜更かしで SAN が毎秒どれだけ減っているか。</summary>
        public float DrainPerSecond
        {
            get
            {
                if (_tuning == null || _dayNight == null)
                {
                    return 0f;
                }

                var late = _dayNight.SecondsSinceNightfall - _tuning.LateNightStartSeconds;
                return late <= 0f
                    ? 0f
                    : Mathf.Min(_tuning.LateNightRampPerSecond * late, _tuning.LateNightMaxDrainPerSecond);
            }
        }

        void Update()
        {
            var vitals = _player != null && _player.Vitals != null ? _player.Vitals.Vitals : null;
            if (vitals == null || _dayNight == null || _dayNight.Clock == null || !vitals.IsAlive)
            {
                return;
            }

            if (_dayNight.Clock.Day != _day)
            {
                _day = _dayNight.Clock.Day;
                _saidDrowsy = false;
                _saidExhausted = false;
            }

            var drain = DrainPerSecond;
            if (drain <= 0f || Time.deltaTime <= 0f)
            {
                return;
            }

            vitals.Apply(new VitalsDelta { Sanity = -drain * Time.deltaTime });

            if (!_saidDrowsy)
            {
                _saidDrowsy = true;
                _player.Say(ProtoText.LateNightDrowsy);
            }
            else if (!_saidExhausted && drain >= _tuning.LateNightMaxDrainPerSecond * 0.5f)
            {
                _saidExhausted = true;
                _player.Say(ProtoText.LateNightExhausted);
            }
        }
    }
}
