using System;

namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// 4つの状態の唯一の権威。HP・渇き・空腹・SAN を保持し、変更を許すのはこの型だけである。
    /// UI・退色・移動速度はいずれもここを観測するだけであり、値を書き換えてはならない。
    ///
    /// SAN はゼロになっても即死させない。HP がゼロになったときは <see cref="Died"/> を1度だけ発火し、
    /// 病院への搬送は外側（<c>HospitalTransport</c>）が <see cref="Revive"/> を呼んで完了させる。
    /// </summary>
    public sealed class Vitals
    {
        readonly VitalsTuning _tuning;

        float _hp;
        float _thirst;
        float _hunger;
        float _sanity;

        public Vitals(VitalsTuning tuning, VitalsState state = null)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));

            _hp = Clamp(state?.Hp ?? _tuning.MaxHp, 0f, _tuning.MaxHp);
            _thirst = Clamp(state?.Thirst ?? _tuning.MaxThirst, 0f, _tuning.MaxThirst);
            _hunger = Clamp(state?.Hunger ?? _tuning.MaxHunger, 0f, _tuning.MaxHunger);
            _sanity = Clamp(state?.Sanity ?? _tuning.MaxSanity, 0f, _tuning.MaxSanity);
        }

        public VitalsTuning Tuning => _tuning;

        public float Hp => _hp;
        public float Thirst => _thirst;
        public float Hunger => _hunger;
        public float Sanity => _sanity;

        public float HpFraction => Fraction(_hp, _tuning.MaxHp);
        public float ThirstFraction => Fraction(_thirst, _tuning.MaxThirst);
        public float HungerFraction => Fraction(_hunger, _tuning.MaxHunger);
        public float SanityFraction => Fraction(_sanity, _tuning.MaxSanity);

        public bool IsAlive => _hp > 0f;

        public SanityTier SanityTier => SanityScale.TierOf(_sanity);

        /// <summary>ワールドの描画にかける彩度。UI には決して適用しない。</summary>
        public float Saturation => SanityScale.Saturation(_sanity);

        /// <summary>渇きがゼロのあいだ移動が遅くなる。</summary>
        public float SpeedMultiplier => _thirst <= 0f ? _tuning.DehydratedSpeedMultiplier : 1f;

        /// <summary>HP がゼロに達した瞬間に1度だけ発火する。搬送が済むまで再発火しない。</summary>
        public event Action Died;

        public void Advance(float deltaSeconds, bool running)
        {
            if (deltaSeconds <= 0f || !IsAlive)
            {
                return;
            }

            var runMultiplier = running ? _tuning.RunDrainMultiplier : 1f;
            var lowHungerRunMultiplier = running && _hunger < _tuning.LowHungerThreshold
                ? _tuning.LowHungerRunDrainMultiplier
                : 1f;

            _thirst = Clamp(_thirst - _tuning.ThirstDrainPerSecond * runMultiplier * deltaSeconds, 0f, _tuning.MaxThirst);
            _hunger = Clamp(
                _hunger - _tuning.HungerDrainPerSecond * runMultiplier * lowHungerRunMultiplier * deltaSeconds,
                0f, _tuning.MaxHunger);
            _sanity = Clamp(_sanity - _tuning.SanityDrainPerSecond * deltaSeconds, 0f, _tuning.MaxSanity);

            var damage = 0f;
            if (_thirst <= 0f)
            {
                damage += _tuning.DehydrationDamagePerSecond * deltaSeconds;
            }

            if (_hunger <= 0f)
            {
                damage += _tuning.StarvationDamagePerSecond * deltaSeconds;
            }

            if (damage > 0f)
            {
                SetHp(_hp - damage);
            }
        }

        /// <summary>飲む・食べる・寝る・傷つく。すべてこの1本の経路を通る。</summary>
        public void Apply(VitalsDelta delta)
        {
            _thirst = Clamp(_thirst + delta.Thirst, 0f, _tuning.MaxThirst);
            _hunger = Clamp(_hunger + delta.Hunger, 0f, _tuning.MaxHunger);
            _sanity = Clamp(_sanity + delta.Sanity, 0f, _tuning.MaxSanity);

            if (delta.Hp != 0f)
            {
                SetHp(_hp + delta.Hp);
            }
        }

        /// <summary>
        /// ソフトクロックが実フレームの外でまとまって進んだぶん（漁り・バイトなどの行動）を、渇きと空腹に
        /// 反映する。時間が経てば喉は渇き、腹は減る。<see cref="Advance"/> と違いダメージは扱わない
        /// （ゼロ割れの遡及ダメージは通常の <see cref="Advance"/> に委ねる）。
        ///
        /// <paramref name="intensity"/> は活動の強度倍率である。待機のまま時間が過ぎるなら 1、バイトの
        /// ように激しく体を使うなら 2 といった値を渡す。待機の消耗と労働の消耗は同じではない。
        ///
        /// SAN はここでは削らない。SAN は実時間の緩やかな摩耗と明示的な代償（バイト）で扱う――
        /// 就寝の一晩ぶんの消費が渇き・空腹だけを削り SAN を削らないのと同じ規則である。
        /// </summary>
        public void DrainTime(float seconds, float intensity = 1f)
        {
            if (seconds <= 0f || intensity <= 0f)
            {
                return;
            }

            _thirst = Clamp(_thirst - _tuning.ThirstDrainPerSecond * seconds * intensity, 0f, _tuning.MaxThirst);
            _hunger = Clamp(_hunger - _tuning.HungerDrainPerSecond * seconds * intensity, 0f, _tuning.MaxHunger);
        }

        /// <summary>
        /// 病院で目を覚ます。HP のみ全回復し、渇き・空腹は死亡直前の値を維持する。
        /// 命は助かるが、目覚めてすぐにまた生存の心配をする状態に戻る。
        /// SAN のペナルティと医療費は呼び出し側が <see cref="Apply"/> と所持金で適用する。
        /// </summary>
        public void Revive()
        {
            _hp = _tuning.MaxHp;
        }

        public VitalsState CaptureState() => new VitalsState
        {
            Hp = _hp,
            Thirst = _thirst,
            Hunger = _hunger,
            Sanity = _sanity,
        };

        void SetHp(float value)
        {
            var wasAlive = IsAlive;
            _hp = Clamp(value, 0f, _tuning.MaxHp);

            if (wasAlive && !IsAlive)
            {
                Died?.Invoke();
            }
        }

        static float Fraction(float value, float max) => max <= 0f ? 0f : Clamp(value / max, 0f, 1f);

        static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
