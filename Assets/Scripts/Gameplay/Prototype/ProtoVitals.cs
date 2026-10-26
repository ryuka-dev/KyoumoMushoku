using System;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片で、4つの状態（HP・渇き・空腹・SAN）を持つ唯一の場所。規則は本編と同じ Core の <see cref="Vitals"/>、
    /// 数値は <c>Assets/Config/VitalsTuning.asset</c>。他のコンポーネントはここを観測するか、<see cref="Vitals.Apply"/> を通す。
    ///
    /// 本編の <see cref="PlayerVitals"/> は 2D の移動（PlayerMotor）に結びついているので、一人称にはこれを置く。
    /// 一日目の朝の SAN は切片の数値（<see cref="SliceTuningAsset.StartingSanity"/>）。満タンからは始めない。
    /// </summary>
    [RequireComponent(typeof(ProtoFirstPersonController))]
    public sealed class ProtoVitals : MonoBehaviour
    {
        [SerializeField] VitalsTuningAsset _tuning;
        [SerializeField] SliceTuningAsset _slice;

        ProtoFirstPersonController _body;

        public Vitals Vitals { get; private set; }

        /// <summary>HP がゼロに達した瞬間に1度だけ発火する。</summary>
        public event Action Died;

        public void Configure(VitalsTuningAsset tuning, SliceTuningAsset slice)
        {
            _tuning = tuning;
            _slice = slice;
        }

        void Awake()
        {
            _body = GetComponent<ProtoFirstPersonController>();
            var tuning = _tuning != null ? _tuning.ToTuning() : new VitalsTuning();
            var start = new VitalsState
            {
                Hp = tuning.MaxHp,
                Thirst = tuning.MaxThirst,
                Hunger = tuning.MaxHunger,
                Sanity = _slice != null ? _slice.StartingSanity : tuning.MaxSanity,
            };
            Vitals = new Vitals(tuning, start);
            Vitals.Died += OnDied;
        }

        void OnDestroy()
        {
            if (Vitals != null)
            {
                Vitals.Died -= OnDied;
            }
        }

        void Update()
        {
            Vitals.Advance(Time.deltaTime, _body.IsRunning);
        }

        void OnDied() => Died?.Invoke();
    }
}
