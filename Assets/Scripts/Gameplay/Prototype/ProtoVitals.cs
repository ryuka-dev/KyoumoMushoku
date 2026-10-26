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
    /// </summary>
    [RequireComponent(typeof(ProtoFirstPersonController))]
    public sealed class ProtoVitals : MonoBehaviour
    {
        [SerializeField] VitalsTuningAsset _tuning;

        ProtoFirstPersonController _body;

        public Vitals Vitals { get; private set; }

        /// <summary>HP がゼロに達した瞬間に1度だけ発火する。</summary>
        public event Action Died;

        public void Configure(VitalsTuningAsset tuning) => _tuning = tuning;

        void Awake()
        {
            _body = GetComponent<ProtoFirstPersonController>();
            Vitals = new Vitals(_tuning != null ? _tuning.ToTuning() : new VitalsTuning());
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
