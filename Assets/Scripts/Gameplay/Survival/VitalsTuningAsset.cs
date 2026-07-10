using KyoumoMushoku.Core.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Survival
{
    /// <summary>
    /// 4つの状態の調整値。すべて叩き台であり、プレイテストで調整する。
    /// 純粋データ <see cref="VitalsTuning"/> を Inspector から編集できるようにするだけの薄い包み。
    /// </summary>
    [CreateAssetMenu(fileName = "VitalsTuning", menuName = "KyoumoMushoku/Vitals Tuning")]
    public sealed class VitalsTuningAsset : ScriptableObject
    {
        [Header("最大値")]
        [SerializeField] float _maxHp = 100f;
        [SerializeField] float _maxThirst = 100f;
        [SerializeField] float _maxHunger = 100f;

        [Header("毎秒の低下量")]
        [SerializeField] float _thirstDrainPerSecond = 0.15f;
        [SerializeField] float _hungerDrainPerSecond = 0.09f;
        [SerializeField] float _sanityDrainPerSecond = 0.025f;

        [Header("走行と空腹")]
        [SerializeField] float _runDrainMultiplier = 1.6f;
        [SerializeField] float _lowHungerThreshold = 30f;
        [SerializeField] float _lowHungerRunDrainMultiplier = 1.5f;

        [Header("ゼロ状態のダメージと減速")]
        [SerializeField] float _dehydrationDamagePerSecond = 1.0f;
        [SerializeField] float _starvationDamagePerSecond = 0.8f;
        [SerializeField] [Range(0.1f, 1f)] float _dehydratedSpeedMultiplier = 0.6f;

        public VitalsTuning ToTuning() => new VitalsTuning
        {
            MaxHp = _maxHp,
            MaxThirst = _maxThirst,
            MaxHunger = _maxHunger,
            MaxSanity = SanityScale.Max,
            ThirstDrainPerSecond = _thirstDrainPerSecond,
            HungerDrainPerSecond = _hungerDrainPerSecond,
            SanityDrainPerSecond = _sanityDrainPerSecond,
            RunDrainMultiplier = _runDrainMultiplier,
            LowHungerThreshold = _lowHungerThreshold,
            LowHungerRunDrainMultiplier = _lowHungerRunDrainMultiplier,
            DehydrationDamagePerSecond = _dehydrationDamagePerSecond,
            StarvationDamagePerSecond = _starvationDamagePerSecond,
            DehydratedSpeedMultiplier = _dehydratedSpeedMultiplier,
        };
    }
}
