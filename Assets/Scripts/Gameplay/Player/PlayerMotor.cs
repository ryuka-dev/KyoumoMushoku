using UnityEngine;

namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// 2D 横スクロールの水平移動。
    /// 移動速度は「街を横断するのに実時間でどれだけかかるか」を決めるため、
    /// 第二節の時間帯の表と合わせて調整する（Phase 0 の検証項目）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float _walkSpeed = 4.5f;
        [SerializeField] float _runSpeed = 7.5f;

        Rigidbody2D _body;
        IPlayerInput _input;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<IPlayerInput>();

            if (_input is null)
            {
                Debug.LogError($"{nameof(PlayerMotor)}: {nameof(IPlayerInput)} の実装が同じ GameObject にない。", this);
                enabled = false;
            }
        }

        public float CurrentSpeed => Mathf.Abs(_body.linearVelocity.x);

        void FixedUpdate()
        {
            var speed = _input.RunHeld ? _runSpeed : _walkSpeed;
            var velocity = _body.linearVelocity;
            _body.linearVelocity = new Vector2(_input.Horizontal * speed, velocity.y);
        }
    }
}
