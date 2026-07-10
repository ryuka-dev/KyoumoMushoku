using System;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Player;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Survival
{
    /// <summary>
    /// <see cref="Vitals"/> を Unity のフレーム更新に接続する唯一の場所であり、4つの状態の所有者。
    /// 他のコンポーネントはここを観測するだけで、<see cref="Vitals"/> を直接持たない。
    ///
    /// 渇きによる減速は <see cref="IMovementSpeedModifier"/> として供給する。
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerVitals : MonoBehaviour, IMovementSpeedModifier
    {
        [SerializeField] VitalsTuningAsset _tuning;

        PlayerMotor _motor;

        public Vitals Vitals { get; private set; }

        public float SpeedMultiplier => Vitals?.SpeedMultiplier ?? 1f;

        /// <summary>HP がゼロに達した瞬間に1度だけ発火する。搬送は外側が担う。</summary>
        public event Action Died;

        public void Configure(VitalsTuningAsset tuning) => _tuning = tuning;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();

            var tuning = _tuning != null ? _tuning.ToTuning() : new VitalsTuning();
            Vitals = new Vitals(tuning);
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
            Vitals.Advance(Time.deltaTime, _motor.IsRunning);
        }

        /// <summary>セーブデータから状態を差し替える。ロードの単一の所有者（GameSession）だけが呼ぶ。</summary>
        public void RestoreState(VitalsState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Vitals.Died -= OnDied;
            Vitals = new Vitals(_tuning != null ? _tuning.ToTuning() : new VitalsTuning(), state);
            Vitals.Died += OnDied;
        }

        void OnDied() => Died?.Invoke();
    }
}
