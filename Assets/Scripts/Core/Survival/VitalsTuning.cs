using System;

namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// 4つの状態の調整値。すべて叩き台であり、プレイテストで調整する。
    ///
    /// 3本のゲージは、低下速度・回復方法・ペナルティがそれぞれ異なる必要がある（第十節）。
    /// 渇きは最も早く低下して短期的な圧力を作り、空腹は中程度、SAN は最も緩やかに低下するが
    /// 最も回復が難しい。全部が「ゼロになったらダメージ」だけになってはならない。
    /// </summary>
    [Serializable]
    public sealed class VitalsTuning
    {
        public float MaxHp = 100f;
        public float MaxThirst = 100f;
        public float MaxHunger = 100f;
        public float MaxSanity = SanityScale.Max;

        /// <summary>渇き。最も早く低下する。約11分でゼロ。</summary>
        public float ThirstDrainPerSecond = 0.15f;

        /// <summary>空腹。中程度の速度。約18分でゼロ。</summary>
        public float HungerDrainPerSecond = 0.09f;

        /// <summary>SAN。最も緩やか。1ゲームデイでおよそ 10〜15 低下する。</summary>
        public float SanityDrainPerSecond = 0.025f;

        /// <summary>走行中、渇きと空腹の低下が速まる。</summary>
        public float RunDrainMultiplier = 1.6f;

        /// <summary>これを下回ると「空腹度が低い」状態。走行時の消耗がさらに増える。</summary>
        public float LowHungerThreshold = 30f;

        /// <summary>空腹度が低いときに走った場合、空腹の低下にさらに掛かる倍率。</summary>
        public float LowHungerRunDrainMultiplier = 1.5f;

        /// <summary>渇きがゼロのあいだ受け続けるダメージ。</summary>
        public float DehydrationDamagePerSecond = 1.0f;

        /// <summary>空腹がゼロのあいだ受け続けるダメージ。</summary>
        public float StarvationDamagePerSecond = 0.8f;

        /// <summary>渇きがゼロのあいだの移動速度倍率。漁りの速度低下は第二層（Phase 2）で扱う。</summary>
        public float DehydratedSpeedMultiplier = 0.6f;

        /// <summary>
        /// 一晩ぶんの渇きの消費。無料の寝床（ベンチ・地下通路）で寝ると起床時にこれだけ渇く。
        /// 就寝は時間の経過であり、一晩が過ぎたぶんの代償を負う。安宿（完全回復）は満タンに戻るので免除。
        /// </summary>
        public float OvernightThirstDrain = 30f;

        /// <summary>一晩ぶんの空腹の消費。無料の寝床でのみ効く（安宿は免除）。</summary>
        public float OvernightHungerDrain = 20f;

        public VitalsTuning Clone() => (VitalsTuning)MemberwiseClone();
    }
}
