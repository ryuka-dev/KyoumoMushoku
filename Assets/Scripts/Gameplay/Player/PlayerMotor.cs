using System.Collections.Generic;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// 2D 横スクロールの水平移動。
    /// 移動速度は「街を横断するのに実時間でどれだけかかるか」を決めるため、
    /// 第二節の時間帯の表と合わせて調整する（Phase 0 の検証項目）。
    ///
    /// 速度の減衰は <see cref="IMovementSpeedModifier"/> を通してのみ受け取り、
    /// 渇きや積載といった個別の理由をこのクラスに持ち込まない。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float _walkSpeed = 4.5f;
        [SerializeField] float _runSpeed = 7.5f;

        readonly List<IMovementSpeedModifier> _modifiers = new();

        Rigidbody2D _body;
        IPlayerInput _input;

        /// <summary>いま走って動いているか。状態の消耗計算が参照する。</summary>
        public bool IsRunning { get; private set; }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<IPlayerInput>();

            if (_input is null)
            {
                Debug.LogError($"{nameof(PlayerMotor)}: {nameof(IPlayerInput)} の実装が同じ GameObject にない。", this);
                enabled = false;
                return;
            }

            GetComponents(_modifiers);
        }

        public float CurrentSpeed => Mathf.Abs(_body.linearVelocity.x);

        void FixedUpdate()
        {
            IsRunning = _input.RunHeld && !Mathf.Approximately(_input.Horizontal, 0f);

            var speed = (_input.RunHeld ? _runSpeed : _walkSpeed) * CombinedMultiplier();
            var velocity = _body.linearVelocity;
            _body.linearVelocity = new Vector2(_input.Horizontal * speed, velocity.y);
        }

        float CombinedMultiplier()
        {
            var multiplier = 1f;
            foreach (var modifier in _modifiers)
            {
                multiplier *= modifier.SpeedMultiplier;
            }

            return multiplier;
        }
    }
}
